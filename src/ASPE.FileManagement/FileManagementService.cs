using ASPE.DQM.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ASPE.FileManaagement
{
    public class FileManagementService : IHostedService
    {
        readonly IFileService _fileService;
        readonly int _hours;
        public FileManagementService(IServiceProvider serviceProvider, IConfiguration config)
        {
            _fileService = (IFileService)serviceProvider.GetRequiredService(Type.GetType(config["Files:Type"]));
            _hours = config.GetValue<int>("Files:TemporaryFileDeletionHours");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _fileService.DeleteTemporaryFilesAsync(_hours);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
