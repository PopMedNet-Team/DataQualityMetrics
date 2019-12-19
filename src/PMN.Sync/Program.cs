using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PMN.Sync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var host = new HostBuilder()
                .ConfigureHostConfiguration(hostConfig => {
                    hostConfig.SetBasePath(AppContext.BaseDirectory);                    
                    hostConfig.AddJsonFile("hostsettings.json", optional: true);
                    hostConfig.AddEnvironmentVariables();
                    hostConfig.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) => {
                    configApp.AddJsonFile("appsettings.json", optional: false);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: false);
                    configApp.AddEnvironmentVariables();
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) => {

                    services.AddDbContext<Data.PMN.DataContext>(options => {
                        options.UseSqlServer(
                            hostContext.Configuration.GetConnectionString("PMN")
                            );
                        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                    });

                    services.AddDbContext<ASPE.DQM.Model.ModelDataContext>(options => {
                        options.UseSqlServer(
                            hostContext.Configuration.GetConnectionString("IdentityContextConnection")
                            );
                    });

                    string synchronizer = hostContext.Configuration.GetValue<string>("Sync:Synchronizer");
                    if(string.Equals(synchronizer, "EntityFramework", StringComparison.OrdinalIgnoreCase))
                    {
                        services.AddTransient<ISynchronizer, EntityFrameworkSynchronizer>();

                        services.AddDbContext<ASPE.DQM.Identity.IdentityContext>(options => {
                            options.UseSqlServer(
                                hostContext.Configuration.GetConnectionString("IdentityContextConnection")
                                );
                        });

                        services.AddDbContext<ASPE.DQM.Sync.SyncDataContext>(options => {
                            options.UseSqlServer(
                                hostContext.Configuration.GetConnectionString("IdentityContextConnection")
                                );
                        });

                        services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<ASPE.DQM.Identity.IdentityUser>, ASPE.DQM.Identity.PasswordHasher>();

                        services.AddDefaultIdentity<ASPE.DQM.Identity.IdentityUser>()
                            .AddEntityFrameworkStores<ASPE.DQM.Identity.IdentityContext>()
                            .AddDefaultTokenProviders();

                        services.Configure<IdentityOptions>(o =>
                        {
                            o.User.AllowedUserNameCharacters = o.User.AllowedUserNameCharacters + " ";
                        });
                    }
                    else if (string.Equals(synchronizer, "Http", StringComparison.OrdinalIgnoreCase))
                    {
                        services.AddTransient<ISynchronizer, HttpSynchronizer>();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid value for the Synchronizer: \"{synchronizer}\". Valid values are \"Http\" or \"EntityFramework\".");
                    }                    

                    services.AddTransient<ISyncService, UserSyncService>();

                    services.AddHostedService<AppService>();                   
                    
                })
                .ConfigureLogging((hostContext, configLogging) => {
                    Log.Logger = new LoggerConfiguration()
                                 .Enrich.FromLogContext()
                                 .ReadFrom.Configuration(hostContext.Configuration)
                                 .CreateLogger();

                    configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    configLogging.AddConsole();
                    configLogging.AddSerilog();

                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        configLogging.AddDebug();
                    }
                })
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                host.Start();

                host.WaitForShutdown();
            }
        }
    }
}
