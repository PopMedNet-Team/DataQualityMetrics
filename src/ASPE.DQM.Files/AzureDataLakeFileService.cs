using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Files
{

    public class AzureDataLakeFileService : BaseFileService, IFileService
    {
        public AzureDataLakeFileService(IConfiguration config, ModelDataContext dataContext, ILogger<AzureDataLakeFileService> logger) : base(config, dataContext, logger) {}

        public async Task FinalizeUploadAsync()
        {
            for (int i = 0; i < _doc.ChunkCount; i++)
            {
                var client = new DataLakeAPI(_config);

                await client.MoveFile(string.Format("{0}_{1}.TMP", MetaData.UploadUid, i), string.Format("{0}_{1}.part", _doc.ID, i));
                _logger.LogDebug(string.Format("File {0}_{1}.TMP renamed to {2}_{1}.part", MetaData.UploadUid, i, _doc.ID));
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

            var client = new DataLakeAPI(_config);
            await client.CreateFile(GetLocalFileName());
            await client.UploadBytes(GetLocalFileName(), file.OpenReadStream());
            await client.FlushBytes(GetLocalFileName(), file.Length);
            _logger.LogDebug("Uploaded the Chunks to Azure for file {0}.", GetLocalFileName());
        }

        public System.IO.Stream ReturnStream(Guid id)
        {
            throw new NotSupportedException();
        }

        public async Task DeleteFileChunksAsync(Guid id)
        {
            var chunkCount = await _dataContext.Documents.Where(x => x.ID == id).Select(x => x.ChunkCount).FirstOrDefaultAsync();

            for (int i = 0; i < chunkCount; i++)
            {
                var client = new DataLakeAPI(_config);

                await client.DeleteFile(string.Format("{0}_{1}.part", id, i));
                _logger.LogDebug("Successfully Deleted the file chunk {0}.", string.Format("{0}_{1}.part", id, i));
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

            var client = new DataLakeAPI(_config);

            var files = await client.ListFiles(id.ToString("D"));

            for (int i = 0; i < files.Count(); i++)
            {
                var file = await client.GetFile(string.Format("{0}_{1}.part", id, i));

                await file.CopyToAsync(response.Body);
            }

            return new OkResult();
        }

        public Task DeleteTemporaryFilesAsync(int hours)
        {
            throw new NotImplementedException();
        }
    }
}
