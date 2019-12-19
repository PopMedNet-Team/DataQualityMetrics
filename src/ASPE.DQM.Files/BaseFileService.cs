using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ASPE.DQM.Files
{
    public abstract class BaseFileService : IFileService
    {
        public BaseFileService(IConfiguration config, ModelDataContext dataContext, ILogger logger)
        {
            _config = config;
            _logger = logger;
            _dataContext = dataContext;
        }

        protected readonly ILogger _logger;
        protected readonly ModelDataContext _dataContext;
        protected IConfiguration _config;

        /// <summary>
        /// Gets the temporary filename of the document.
        /// </summary>
        /// <param name="identifier">The document identifier.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns></returns>
        public virtual string GetTempFileName(string identifier, uint chunkIndex)
        {
            return string.Format("{0}_{1}.TMP", identifier, chunkIndex);
        }
        /// <summary>
        /// Gets the permanent filename of the document.
        /// </summary>
        /// <param name="identifier">The document identifier.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns></returns>
        public virtual string GetFileName(string identifier, uint chunkIndex)
        {
            return string.Format("{0}_{1}.part", identifier, chunkIndex);
        }

        /// <summary>
        /// Gets the permanent filename of the document.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <returns></returns>
        protected string GetFileName(Guid id, uint chunkIndex)
        {
            return GetFileName(id.ToString("D"), chunkIndex);
        }

        /// <summary>
        /// Initiates the document object to be saved in the database
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Document> SaveDocumentToDatabaseAsync(ModelDataContext dataContext, ChunkMetaData metadata, Guid itemID, Guid uploadByID)
        {
            var doc = new Document
            {
                Name = metadata.FileName,
                FileName = metadata.FileName,
                MimeType = FileEx.GetMimeTypeByExtension(metadata.FileName),
                ItemID = itemID,
                Length = metadata.TotalFileSize,
                UploadedByID = uploadByID,
                ChunkCount = metadata.TotalChunks
            };

            dataContext.Documents.Add(doc);

            await dataContext.SaveChangesAsync();

            _logger.LogInformation($"Saved the document metadata to the database for { metadata.FileName }, ID: { doc.ID.ToString("D") }.");

            return doc;
        }

        public abstract Task WriteToStreamAsync(IFormFile file, ChunkMetaData uploadMetadata);

        public abstract Task WriteToStreamAsync(string identifier, uint chunkIndex, System.IO.Stream source);

        public abstract Task DeleteTemporaryFilesAsync(int hours);

        public abstract Task FinalizeUploadAsync(Document document, ChunkMetaData metadata);

        public abstract Stream ReturnStream(Guid id);

        public abstract System.IO.Stream ReturnTempFileStream(string identifier);

        public abstract Task<IActionResult> ReturnDownloadActionResultAsync(Guid id, HttpResponse response);

        public abstract Task DeleteFileChunksAsync(Guid id);

        public abstract Task DeleteTempFileChunkAsync(string identifier);
    }

    /// <summary>
    /// The Result that should be sent back to the Upload Control
    /// </summary>
    public class UploadChuckResult
    {
        /// <summary>
        /// Determines if all the chunks have been uploaded
        /// </summary>
        public bool uploaded { get; set; }
        /// <summary>
        /// The File UID sent by the upload control
        /// </summary>
        public string fileUid { get; set; }
    }

    /// <summary>
    /// The MetaData sent by the Upload Control
    /// </summary>
    public class ChunkMetaData
    {
        /// <summary>
        /// The File UID sent by the upload control
        /// </summary>
        public string UploadUid { get; set; }
        /// <summary>
        /// The File Name sent by the upload control
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// The File Name without the Extension sent by the upload control
        /// </summary>
        public string FileNameWithOutExtension { get { return Path.GetFileNameWithoutExtension(FileName); } }
        /// <summary>
        /// The File Extension sent by the upload control
        /// </summary>
        public string FileExtension { get { return Path.GetExtension(FileName); } }
        /// <summary>
        /// The Content Type sent by the upload control
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// The Chunk Index sent by the upload control
        /// </summary>
        public int ChunkIndex { get; set; }
        /// <summary>
        /// The Total Chunks sent by the upload control
        /// </summary>
        public int TotalChunks { get; set; }
        /// <summary>
        /// The File size sent by the upload control
        /// </summary>
        public long TotalFileSize { get; set; }
        /// <summary>
        /// Gets if the current chunk is the final chunk.
        /// </summary>
        public bool IsFinalChunck
        {
            get
            {
                return TotalChunks - 1 <= ChunkIndex;
            }
        }
    }
}
