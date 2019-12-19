using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ASPE.DQM.Files
{
    public interface IFileService
    {
        /// <summary>
        /// Gets the temporary file name.
        /// </summary>
        string GetTempFileName(string identifier, uint chunkIndex);

        /// <summary>
        /// Gets the permanent file name.
        /// </summary>
        string GetFileName(string identifier, uint chunkIndex);
        
        /// <summary>
        /// Persists the file chunks to storage as temporary files. Calling <see cref="FinalizeUploadAsync"/> will convert the files from temporary to permanent.
        /// </summary>
        /// <param name="file">The File that is supposed to be streamed.</param>
        /// <returns></returns>
        Task WriteToStreamAsync(IFormFile file, ChunkMetaData uploadMetadata);

        /// <summary>
        /// Persists the source stream to storage as a temporary file. Calling <see cref="FinalizeUploadAsync"/> will convert the files from temporary to permanent.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="chunkIndex"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task WriteToStreamAsync(string identifier, uint chunkIndex, System.IO.Stream source);

        /// <summary>
        /// Saves the document metadata to the database.
        /// </summary>
        /// <param name="dataContext"></param>
        /// <param name="itemID"></param>
        /// <param name="metadata"></param>
        /// <param name="uploadByID"></param>
        /// <returns></returns>
        Task<Document> SaveDocumentToDatabaseAsync(ModelDataContext dataContext, ChunkMetaData metadata, Guid itemID, Guid uploadByID);

        /// <summary>
        /// Finalizes the chunked upload process, converting the chunks from temp files to permanently persisted files.
        /// </summary>
        /// <param name="document">The document to finalize the physical file chunks for.</par
        /// <param name="metadata">The upload metadata about the document being finalized.</m>
        /// <returns></returns>
        Task FinalizeUploadAsync(Document document, ChunkMetaData metadata);

        /// <summary>
        /// Returns a single stream aggregating all the individual chunk streams for the specified document.
        /// </summary>
        /// <returns></returns>
        System.IO.Stream ReturnStream(Guid id);

        /// <summary>
        /// Returns a single stream aggregating all the individual chunk streams for the specified temporary file.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        System.IO.Stream ReturnTempFileStream(string identifier);

        /// <summary>
        /// Deletes all the chunks from the file service for the specified document.
        /// </summary>
        Task DeleteFileChunksAsync(Guid id);

        /// <summary>
        /// Deletes all the chunks from the file service for the specified temporary file.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task DeleteTempFileChunkAsync(string identifier);

        /// <summary>
        /// Deletes all temporary files that are older than the specified amount of hours.
        /// </summary>
        /// <param name="hours">The amount of hours the temp files should remain on disk for.</param>
        /// <returns></returns>
        Task DeleteTemporaryFilesAsync(int hours);        

        /// <summary>
        /// Returns an ActionResult with the FileStream.
        /// </summary>
        /// <param name="id">The ID of the persisted document.</param>
        /// <returns></returns>
        Task<IActionResult> ReturnDownloadActionResultAsync(Guid id, HttpResponse response);

        
    }
}
