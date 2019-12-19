using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ASPE.DQM
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging((hostingContext, logging) => {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddSerilog();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
                .UseStartup<Startup>();
    }
}
