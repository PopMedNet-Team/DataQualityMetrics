using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PMN.Sync.Tests
{
    public static class Helpers
    {

        public static IConfigurationRoot GetConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();
        }

        //public static Identity.IdentityContext CreateIdentityContext()
        //{
        //    var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Identity.IdentityContext>().UseSqlServer(
        //            Helpers.GetConfigurationRoot().GetConnectionString("IdentityContextConnection"),
        //            x => x.MigrationsAssembly("ASPE.DQM.Data")
        //            .EnableRetryOnFailure()
        //            .CommandTimeout(25))
        //            .EnableSensitiveDataLogging()
        //            .Options;

        //    return new Identity.IdentityContext(options);
        //}

    }

    public class ApplicationFactory
    {
        public readonly IConfiguration Configuration;
        public readonly IServiceProvider Provider;
        public readonly Microsoft.AspNetCore.Builder.IApplicationBuilder Builder;

        public ApplicationFactory()
        {
            var config = new ConfigurationBuilder()
                    .SetBasePath(System.AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config)
                .AddDbContext<PMN.Sync.Data.PMN.DataContext>(o =>
                {
                    o.UseSqlServer(config.GetConnectionString("PMN_DataContext"));
                    o.UseQueryTrackingBehavior(Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking);
                });

            services.AddLogging(lc => {
                lc.AddConfiguration(config.GetSection("Logging"));
                lc.AddConsole();
                lc.AddDebug();
            });

            Provider = services.BuildServiceProvider();
            Builder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(Provider);
        }

        public IServiceScope CreateScope()
        {
            return Provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
