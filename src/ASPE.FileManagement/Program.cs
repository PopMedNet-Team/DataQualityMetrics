using ASPE.DQM.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ASPE.FileManaagement
{
    class Program
    {
        static async Task Main(string[] args)
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
                    services.AddDbContext<ASPE.DQM.Model.ModelDataContext>(options => {
                        options.UseSqlServer(
                            hostContext.Configuration.GetConnectionString("IdentityContextConnection")
                            );
                    });
                    services.AddTransient(Type.GetType(hostContext.Configuration["Files:Type"]));
                    services.AddHostedService<FileManagementService>();
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
                await host.StartAsync();
            }
        }
    }
}
