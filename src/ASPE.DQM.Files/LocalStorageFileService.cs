using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASPE.DQM.Files
{
    public class LocalStorageFileService : BaseFileService
    {
        public LocalStorageFileService(IConfiguration config, ModelDataContext dataContext, ILogger<LocalStorageFileService> logger) : base(config, dataContext, logger)
        {
            if (string.IsNullOrEmpty(config["Files:UploadDirectory"]) || string.IsNullOrWhiteSpace(config["Files:UploadDirectory"]))
            {
                _logger.LogCritical("The Configuration for the Upload directory has not been set.");
                throw new Exception("The Configuration for the Upload directory has not been set.");
            }
            else
            {
                _rootDir = config["Files:UploadDirectory"];
            }
        }
        /// <summary>
        /// The Directory the Files should be placed into.
        /// </summary>
        readonly string _rootDir;

        public override async Task FinalizeUploadAsync(Document document, ChunkMetaData uploadMetadata)
        {
            for (uint i = 0; i < document.ChunkCount; i++)
            {
                string tempFilename = GetTempFileName(uploadMetadata.UploadUid, i);
                string fileName = GetFileName(document.ID, i);

                File.Move(Path.Combine(_rootDir, tempFilename), Path.Combine(_rootDir, fileName));
                _logger.LogDebug(string.Format("File {0} renamed to {1}", tempFilename, fileName));
            }
        }

        public override async Task WriteToStreamAsync(IFormFile file, ChunkMetaData uploadMetadata)
        {
            string filename = GetTempFileName(uploadMetadata.UploadUid, Convert.ToUInt32(uploadMetadata.ChunkIndex));
            var filePath = Path.Combine(_rootDir, filename);

            _logger.LogDebug("Creating the file {0} on local storage.", filename);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                _logger.LogDebug("Uploaded chunks to the file {0} local storage.", filename);
            }
        }

        public override async Task WriteToStreamAsync(string identifier, uint chunkIndex, Stream source)
        {
            string filename = GetTempFileName(identifier, chunkIndex);
            var filePath = Path.Combine(_rootDir, filename);

            _logger.LogDebug("Creating the file {0} on local storage.", filename);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await source.CopyToAsync(stream);
                _logger.LogDebug("Copied stream to the file {0} local storage.", filename);
            }
        }

        public override Stream ReturnStream(Guid id)
        {
            List<Stream> streams = new List<Stream>();

            _logger.LogDebug("Iterating over all the files and adding their streams to the response stream for ID {0}", id.ToString("D"));
            for (uint i = 0; i <= GetDocuments(id).Count() - 1; i++)
            {
                var file = new FileStream(Path.Combine(_rootDir, GetFileName(id, i)), FileMode.Open, FileAccess.Read);
                streams.Add(file);
            }

            return new CombinationStream(streams);
        }

        public override Stream ReturnTempFileStream(string identifier)
        {
            _logger.LogDebug("Iterating over all the files and adding their streams to the response stream for ID {0}", identifier);

            var streams = GetTempDocuments(identifier).Select((f, i) => new FileStream(Path.Combine(_rootDir, GetTempFileName(identifier, Convert.ToUInt32(i))), FileMode.Open, FileAccess.Read)).Cast<Stream>();

            return new CombinationStream(streams.ToList());
        }

        public override async Task DeleteFileChunksAsync(Guid id)
        {
            for (uint i = 0; i <= GetDocuments(id).Count() - 1; i++)
            {
                string filename = GetFileName(id, i);
                File.Delete(Path.Combine(_rootDir, filename));
                _logger.LogInformation(string.Format("Successfully deleted the file chunk {0}.", filename));
            }
        }

        public override async Task DeleteTempFileChunkAsync(string identifier)
        {
            for (uint i = 0; i <= GetTempDocuments(identifier).Count() - 1; i++)
            {
                string filename = GetTempFileName(identifier, i);
                File.Delete(Path.Combine(_rootDir, filename));
                _logger.LogInformation(string.Format("Successfully deleted the file chunk {0}.", filename));
            }
        }

        public override async Task<IActionResult> ReturnDownloadActionResultAsync(Guid id, HttpResponse response)
        {
            var doc = await _dataContext.Documents.Where(x => x.ID == id).Select(x => new  { x.MimeType, x.FileName }).FirstOrDefaultAsync();

            if (doc == null)
            {
                _logger.LogError("Did not find the requested document in the database for ID {0}.", id.ToString("D"));
                return new NotFoundResult();
            }
            
            return new FileStreamResult(ReturnStream(id), doc.MimeType)
            {
                FileDownloadName = doc.FileName,
            };
        }

        string[] GetDocuments(Guid id)
        {
            return Directory.GetFiles(_rootDir, $"{ id.ToString("D") }_*.part");
        }

        string[] GetTempDocuments(string identifier)
        {
            return Directory.GetFiles(_rootDir, $"{ identifier }_*.TMP");
        }

        public override async Task DeleteTemporaryFilesAsync(int hours)
        {
            var files = Directory.GetFiles(_rootDir, "*.TMP");

            foreach (var fileName in files)
            {
                var file = new FileInfo(fileName);

                if(file.LastWriteTime < DateTime.Now.AddHours(hours * -1))
                {
                    file.Delete();
                    _logger.LogInformation($"Deleted { file.Name } from local file storage.");
                }
            }
        }
    }
}
