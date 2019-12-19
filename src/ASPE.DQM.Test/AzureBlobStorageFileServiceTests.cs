using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASPE.DQM.Test
{
    [TestClass]
    public class AzureBlobStorageFileServiceTests
    {
        readonly ApplicationFactory appFactory;

        public AzureBlobStorageFileServiceTests()
        {
            appFactory = new ApplicationFactory((Microsoft.Extensions.DependencyInjection.IServiceCollection services) => {
                services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(Files.IFileService), typeof(Files.AzureBlobStorageFileService), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient));
            });
        }

        [TestMethod]
        public async Task WriteSingleFileToStorage()
        {   
            using (var scope = appFactory.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));

                string identifier = Guid.NewGuid().ToString("D");
                using(var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes(identifier)))
                {
                    await service.WriteToStreamAsync(identifier, 0, ms);
                }


                using (var ms = new System.IO.MemoryStream()) {

                    using (var reader = service.ReturnTempFileStream(identifier))
                    {
                        reader.CopyTo(ms);
                    }

                    string value = Encoding.Default.GetString(ms.ToArray());
                    Assert.AreEqual(identifier, value);
                }

            }


        }

        [TestMethod]
        public async Task WriteMultipleChunksToStorage()
        {
            using (var scope = appFactory.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));

                string identifier = Guid.NewGuid().ToString("D");
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("This is ")))
                {
                    await service.WriteToStreamAsync(identifier, 0, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("the start of what ")))
                {
                    await service.WriteToStreamAsync(identifier, 1, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("should be a full sentence.")))
                {
                    await service.WriteToStreamAsync(identifier, 2, ms);
                }

                using (var ms = new System.IO.MemoryStream())
                {

                    using (var reader = service.ReturnTempFileStream(identifier))
                    {
                        reader.CopyTo(ms);
                    }

                    string value = Encoding.Default.GetString(ms.ToArray());
                    Assert.AreEqual("This is the start of what should be a full sentence.", value);
                }

            }
        }

        [TestMethod]
        public async Task DeleteChunks()
        {
            using(var scope = appFactory.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));

                string identifier = Guid.NewGuid().ToString("D");
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("This is ")))
                {
                    await service.WriteToStreamAsync(identifier, 0, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("the start of what ")))
                {
                    await service.WriteToStreamAsync(identifier, 1, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("should be a full sentence.")))
                {
                    await service.WriteToStreamAsync(identifier, 2, ms);
                }

                await service.DeleteTempFileChunkAsync(identifier);

            }
        }

        [TestMethod]
        public async Task DeleteAllTempChunks()
        {
            using(var scope = appFactory.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));

                string identifier = Guid.NewGuid().ToString("D");
                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("This is ")))
                {
                    await service.WriteToStreamAsync(identifier, 0, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("the start of what ")))
                {
                    await service.WriteToStreamAsync(identifier, 1, ms);
                }

                using (var ms = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes("should be a full sentence.")))
                {
                    await service.WriteToStreamAsync(identifier, 2, ms);
                }

                await service.DeleteTemporaryFilesAsync(0);
            }
        }

    }
}
