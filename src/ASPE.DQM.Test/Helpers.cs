using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ASPE.DQM.Test
{
    //public static class Helpers
    //{

    //    public static IConfigurationRoot GetConfigurationRoot()
    //    {
    //        return new ConfigurationBuilder()
    //            .SetBasePath(AppContext.BaseDirectory)
    //            .AddJsonFile("appsettings.json", optional: false)
    //            .AddEnvironmentVariables()
    //            .Build();
    //    }

    //    public static Identity.IdentityContext CreateIdentityContext()
    //    {
    //        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Identity.IdentityContext>().UseSqlServer(
    //                Helpers.GetConfigurationRoot().GetConnectionString("IdentityContextConnection"),
    //                x => x.MigrationsAssembly("ASPE.DQM.Data")
    //                .EnableRetryOnFailure()
    //                .CommandTimeout(25))
    //                .EnableSensitiveDataLogging()
    //                .Options;

    //        return new Identity.IdentityContext(options);
    //    }

    //}

    public class ApplicationFactory
    {
        public readonly IConfiguration Configuration;
        public readonly IServiceProvider Provider;
        public readonly Microsoft.AspNetCore.Builder.IApplicationBuilder Builder;

        public static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        public ApplicationFactory() : this(null) { }

        public ApplicationFactory(Action<IServiceCollection> additionalServices)
        {
            var config = new ConfigurationBuilder()
                    .SetBasePath(System.AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config)
                    .AddDbContext<Model.ModelDataContext>(o => o.UseSqlServer(config.GetConnectionString("IdentityContextConnection"), x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure().CommandTimeout(25)).UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging())
                    .AddDbContext<Identity.IdentityContext>(o => o.UseSqlServer(config.GetConnectionString("IdentityContextConnection"), x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure().CommandTimeout(25)).EnableSensitiveDataLogging())
                    .AddDbContext<Sync.SyncDataContext>(o => o.UseSqlServer(config.GetConnectionString("IdentityContextConnection"), x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure().CommandTimeout(25)).EnableSensitiveDataLogging())
                    .AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<Identity.IdentityUser>, Identity.PasswordHasher>()
                    .AddIdentityCore<Identity.IdentityUser>(o => { })
                    .AddEntityFrameworkStores<Identity.IdentityContext>();

            services.AddLogging();

            additionalServices?.Invoke(services);

            Provider = services.BuildServiceProvider();
            Builder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(Provider);
        }

        public IServiceScope CreateScope()
        {
            return Provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
