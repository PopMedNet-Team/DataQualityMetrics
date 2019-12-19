using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PMN.Sync
{
    internal class AppService : IHostedService
    {
        readonly ILogger _logger;
        readonly IApplicationLifetime _appLifetime;
        readonly ISyncService _userSync;

        public AppService(ILogger<AppService> logger, IApplicationLifetime appLifetime, ISyncService userSync)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _userSync = userSync;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //do shutdown tasks if needed.
            return Task.CompletedTask;
        }

        void OnStarted()
        {
            //_logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here

            _userSync.Run();
        }

        void OnStopping()
        {
            //_logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here

            _userSync.Stop();
        }

        void OnStopped()
        {
            //_logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
}
