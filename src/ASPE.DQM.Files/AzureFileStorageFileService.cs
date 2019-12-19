using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Files
{
    public class AzureFileStorageFileService : BaseFileService, IFileService
    {
        public AzureFileStorageFileService(IConfiguration config, ModelDataContext dataContext, ILogger<AzureFileStorageFileService> logger) : base(config, dataContext, logger) { }

        public async Task FinalizeUploadAsync()
        {
            var share = GetCloudShare();
            
            if (await share.ExistsAsync())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                for (int i = 0; i < _doc.ChunkCount; i++)
                {
                    var fileRef = rootDir.GetFileReference(string.Format("{0}_{1}.TMP", MetaData.UploadUid, i));
                    var newFileRef = rootDir.GetFileReference(string.Format("{0}_{1}.part", _doc.ID, i));

                    await newFileRef.StartCopyAsync(fileRef);
                    _logger.LogDebug(string.Format("File {0}_{1}.TMP being renamed to {2}_{1}.part", MetaData.UploadUid, i, _doc.ID));
                    await fileRef.DeleteAsync();
                    _logger.LogDebug(string.Format("Deleteing File {0}_{1}.TMP", MetaData.UploadUid, i));
                }
            }
            else
            {
                _logger.LogError("The Share specified in the config file does not exists within the Storage Account. The Specified Share in the config file is {0}", _config["Files:FileStorageShare"]);
                throw new Exception("The Share is not configured correctly.");
            }
        }

        public async Task WriteToStreamAsync(IFormFile file, HttpRequest request, User identityUser)
        {
            MetaData = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkMetaData>(request.Form["metadata"]);
            _filename = MetaData.FileName;

            if (MetaData.TotalChunks - 1 <= MetaData.ChunkIndex)
            {
                if (!string.IsNullOrWhiteSpace(request.Form["ItemID"]))
                    Guid.TryParse(request.Form["ItemID"], out _itemID);
            }

            _identityUser = identityUser;

            var share = GetCloudShare();
            if (await share.ExistsAsync())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                var fileRef = rootDir.GetFileReference(GetLocalFileName());
                _logger.LogDebug("Created the File {0} on Azure File Storage.", GetLocalFileName());

                fileRef.UploadFromStream(file.OpenReadStream());
                _logger.LogDebug("Uploaded the Chunks to Azure for file {0}.", GetLocalFileName());
            }
            else
            {
                _logger.LogError("The Share specified in the config file does not exists within the Storage Account. The Specified Share in the config file is {0}", _config["Files:FileStorageShare"]);
                throw new Exception("The Share is not configured correctly.");
            }
        }

        public System.IO.Stream ReturnStream(Guid id)
        {
            throw new NotSupportedException();
        }

        private IEnumerable<IListFileItem> GetDocuments(Guid id)
        {
            var share = GetCloudShare();
            if (share.Exists())
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                var files = rootDir.ListFilesAndDirectories(prefix: id.ToString("D"));
                return files;
            }
            else
            {
                throw new Exception("The Share is not configured correctly.");
            }
        }

        public async Task DeleteFileChunksAsync(Guid id)
        {
            var chunkCount = await _dataContext.Documents.Where(x => x.ID == id).Select(x => x.ChunkCount).FirstOrDefaultAsync();

            for (int i = 0; i < chunkCount; i++)
            {
                var share = GetCloudShare();
                _logger.LogDebug("Verifing that the share exists.");
                if (share.Exists())
                {
                    // Get a reference to the root directory for the share.
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                    var fileRef = rootDir.GetFileReference(string.Format("{0}_{1}.part", id, i));

                    await fileRef.DeleteIfExistsAsync();
                    _logger.LogDebug("Successfully Deleted the file chunk {0}.", string.Format("{0}_{1}.part", id, i));
                }
                else
                {
                    _logger.LogError("The Share specified in the config file does not exists within the Storage Account. The Specified Share in the config file is {0}", _config["Files:FileStorageShare"]);
                    throw new Exception("The Share is not configured correctly.");
                }
            }
        }

        public async Task<IActionResult> ReturnDownloadActionResultAsync(Guid id, HttpResponse response)
        {
            var doc = await _dataContext.Documents.Where(x => x.ID == id).Select(x => new { x.MimeType, x.FileName, x.Length }).FirstOrDefaultAsync();

            if (doc == null)
            {
                _logger.LogError("Did not find the requested document in the database for the ID {0}.", id.ToString("D"));
                return new NotFoundResult();
            }

            _logger.LogDebug("Setting up the Response with Attachment for ID {0}.", id.ToString("D"));
            response.ContentType = "application/octet-stream";
            response.ContentLength = doc.Length;
            var cd = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = doc.FileName,
                Size = doc.Length
            };

            response.Headers.Add("Content-Disposition", cd.ToString());

            var share = GetCloudShare();

            var count = GetDocuments(id).Count();

            _logger.LogDebug("Verifing that the share exists.");
            if (share.Exists())
            {
                for (int i = 0; i < count; i++)
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                    var fileRef = rootDir.GetFileReference(string.Format("{0}_{1}.part", id, i));

                    await fileRef.DownloadToStreamAsync(response.Body);
                    _logger.LogDebug("Added {0}_{1}.part to the Response Stream.", id, i);
                }
            }
            else
            {
                _logger.LogError("The Share specified in the config file does not exists within the Storage Account. The Specified Share in the config file is {0}", _config["Files:FileStorageShare"]);
                throw new Exception("The Share is not configured correctly.");
            }

            return new OkResult();
        }

        private CloudFileShare GetCloudShare()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config["Files:StorageConnectionString"]);

            // Create a CloudFileClient object for credentialed access to Azure Files.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            return fileClient.GetShareReference(_config["Files:FileStorageShare"]);
        }

        public async Task DeleteTemporaryFilesAsync(int hours)
        {
            var container = GetCloudShare();
            if (container.Exists())
            {
                CloudFileDirectory rootDir = container.GetRootDirectoryReference();
                var files = rootDir.ListFilesAndDirectories().OfType<CloudFile>();

                foreach (CloudFile file in files.Where(x => x.Name.EndsWith(".TMP")))
                {
                    if (file.Properties.LastModified.HasValue && file.Properties.LastModified < DateTime.Now.AddHours(hours * -1))
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            else
            {
                throw new Exception("The Container is not configured correctly.");
            }
        }
    }
}
