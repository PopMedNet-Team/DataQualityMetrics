using ASPE.DQM.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Files
{
    public class AzureBlobStorageFileService : BaseFileService
    {
        readonly CloudBlobContainer _container;
        
        public AzureBlobStorageFileService(IConfiguration config, ModelDataContext dataContext, ILogger<AzureBlobStorageFileService> logger) : base(config, dataContext, logger)
        {
            string connectionString = _config["Files:StorageConnectionString"];
            string fileStorageShare = _config["Files:FileStorageShare"];

            if(string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(fileStorageShare))
            {
                _logger.LogCritical("The configuration of the Blob Storage has not been completed.");
                throw new Exception("The configuration of the Blob Storage has not been completed.");
            }

            _logger.LogDebug("Accessing the storage account for Azure Blob Storage.");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            _logger.LogDebug("Creating the Blob Client for Azure Blob Storage.");
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();

            _logger.LogDebug("Confirming the storage container \"" + fileStorageShare + "\" exists.");
            _container = cloudBlobClient.GetContainerReference(_config["Files:FileStorageShare"]);
            if (!_container.Exists())
            {
                _logger.LogCritical("The Container specified in the config file does not exists within the Storage Account. The Specified Container in the config file is {0}", fileStorageShare);
                throw new Exception("The Share is not configured correctly.");
            }
        }

        const string TEMP_PREFIX = "TMP_";
        public override string GetTempFileName(string identifier, uint chunkIndex)
        {
            return base.GetTempFileName(TEMP_PREFIX + identifier, chunkIndex);
        }

        public override async Task FinalizeUploadAsync(Document document, ChunkMetaData uploadMetadata)
        {
            for (uint i = 0; i < document.ChunkCount; i++)
            {
                string tempFilename = GetTempFileName(uploadMetadata.UploadUid, i);
                string fileName = GetFileName(document.ID, i);

                var fileRef = _container.GetBlockBlobReference(tempFilename);
                var newFileRef = _container.GetBlockBlobReference(fileName);

                await newFileRef.StartCopyAsync(fileRef);
                _logger.LogDebug(string.Format("File {0} renamed to {1}", tempFilename, fileName));

                await fileRef.DeleteAsync();
                _logger.LogDebug(string.Format("Deleteing temp file {0}", tempFilename));
            }
        }

        public override async Task WriteToStreamAsync(IFormFile file, ChunkMetaData uploadMetadata)
        {
            string tempFileName = GetTempFileName(uploadMetadata.UploadUid, Convert.ToUInt32(uploadMetadata.ChunkIndex));

            var fileRef = _container.GetBlockBlobReference(tempFileName);
            _logger.LogDebug("File {0} created in Azure Blob Storage.", tempFileName);

            await fileRef.UploadFromStreamAsync(file.OpenReadStream());
            _logger.LogDebug("Uploaded the chunks to Azure Blob Storage for file {0}.", tempFileName);
        }

        public override async Task WriteToStreamAsync(string identifier, uint chunkIndex, Stream source)
        {
            string tempFileName = GetTempFileName(identifier, chunkIndex);

            var fileRef = _container.GetBlockBlobReference(tempFileName);
            _logger.LogDebug("File {0} created in Azure Blob Storage.", tempFileName);

            await fileRef.UploadFromStreamAsync(source);
            _logger.LogDebug("Uploaded the chunks to Azure Blob Storage for file {0}.", tempFileName);
        }

        public override System.IO.Stream ReturnStream(Guid id)
        {
            var blobs = GetDocuments(id).ToArray();

            var stream = new BlobStorageCombinationStreamReader(blobs);

            return stream;
        }

        public override System.IO.Stream ReturnTempFileStream(string identifier)
        {
            var blobs = GetDocuments(TEMP_PREFIX + identifier).ToArray();

            var stream = new BlobStorageCombinationStreamReader(blobs);

            return stream;

        }

        /// <summary>
        /// Gets all the document chunks for the permanent file with the specified id.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns></returns>
        IEnumerable<CloudBlockBlob> GetDocuments(Guid id)
        {
            return GetDocuments(id.ToString("D"));
        }

        /// <summary>
        /// Gets all the document chunks for the files matching the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix of the blob files to return.</param>
        /// <returns></returns>
        IEnumerable<CloudBlockBlob> GetDocuments(string prefix)
        {
            return _container.ListBlobs(prefix: prefix).Cast<CloudBlockBlob>();
        }

        public override async Task DeleteFileChunksAsync(Guid id)
        {
            var chunks = GetDocuments(id).ToArray();
            foreach(var chunk in chunks)
            {
                if (await chunk.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, AccessCondition.GenerateIfExistsCondition(), null, null))
                {
                    _logger.LogDebug("Successfully deleted the file chunk {0} from Azure Blob Storage.", chunk.Name);
                }
            }

        }

        public override async Task DeleteTempFileChunkAsync(string identifier)
        {
            var chunks = GetDocuments(TEMP_PREFIX + identifier).ToArray();

            foreach(var chunk in chunks)
            {
                if (await chunk.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, AccessCondition.GenerateIfExistsCondition(), null, null))
                {
                    _logger.LogDebug("Successfully deleted the file chunk {0} from Azure Blob Storage.", chunk.Name);
                }
            }
        }

        public override async Task<IActionResult> ReturnDownloadActionResultAsync(Guid id, HttpResponse response)
        {
            var doc = await _dataContext.Documents.Where(x => x.ID == id).Select(x => new { x.MimeType, x.FileName, x.Length }).FirstOrDefaultAsync();

            if (doc == null)
            {
                _logger.LogError("Did not find the requested document in the database for the ID {0:D}.", id);
                return new NotFoundResult();
            }

            _logger.LogDebug("Setting up the Response with Attachment for document ID {0:D}.", id);

            response.ContentType = "application/octet-stream";
            response.ContentLength = doc.Length;
            var cd = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = doc.FileName,
                Size = doc.Length
            };

            response.Headers.Add("Content-Disposition", cd.ToString());

            var chunks = GetDocuments(id).ToArray();
            foreach(var chunk in chunks)
            {
                await chunk.DownloadToStreamAsync(response.Body);
            }

            return new OkResult();
        }

        public override async Task DeleteTemporaryFilesAsync(int hours)
        {
            var files = _container.ListBlobs(prefix: TEMP_PREFIX).OfType<CloudBlob>()
                .Where(b => b.Name.EndsWith(".TMP") && b.Properties.LastModified.HasValue && b.Properties.LastModified.Value < DateTimeOffset.Now.AddHours(hours * -1));

            foreach(var file in files)
            {
                if (await file.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, AccessCondition.GenerateIfExistsCondition(), null, null))
                {
                    _logger.LogInformation($"Temporary file { file.Name } deleted from Azure Blob Storage as part of cleanup due to being older than { hours } hours.");
                }
            }
        }
    }

    public class BlobStorageCombinationStreamReader : System.IO.Stream
    {
        readonly HashSet<ChunkItem> _chunks;
        readonly bool _supportSeek;

        /// <summary>
        /// Initializes the BlobStorage Combination StreamReader that will read all the chunk streams as a single stream.
        /// </summary>
        /// <param name="chunks">The chunks to include in the stream.</param>
        /// <param name="supportSeek">Indicates if seeking should be supported, by default false. **Note: the OpenXml Spreadsheet reader does not work correctly when seeking is enabled.</param>
        public BlobStorageCombinationStreamReader(IEnumerable<ICloudBlob> chunks, bool supportSeek = false)
        {
            _chunks = new HashSet<ChunkItem>();
            foreach (var chunk in chunks)
            {
                _chunks.Add(new ChunkItem { Chunk = chunk, StartIndex = _length, EndIndex = _length + (chunk.Properties.Length - 1) });
                _length += chunk.Properties.Length;
            }
            _supportSeek = supportSeek;
        }

        public override bool CanRead {
            get
            {
                return _position < _length;
            }
        }

        public override bool CanSeek => _supportSeek;

        public override bool CanWrite => false;

        long _length = 0;
        public override long Length => _length;

        long _position = 0;
        public override long Position
        {
            get => _position;
            set
            {
                _position = value;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads a sequence of bytes from the source streams, and advances the posistion within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the source.</param>
        /// <param name="offset">The zero-based offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            System.Console.WriteLine($"Position:{_position}, buffer:{ buffer.Length}, offset:{ offset }, count:{ count }");

            if (_position >= Length - 1 || count < 1)
                return 0;

            int totalBytesRead = 0;

            //get the current chunk based on current overall position
            var chunk = _chunks.FirstOrDefault(c => c.StartIndex <= _position && _position <= c.EndIndex);
            if (chunk == null)
            {
                throw new ArgumentOutOfRangeException("The position of the stream is past the end of the available bytes.");
            }

            long positionInChunk = _position - chunk.StartIndex;

            Console.WriteLine($"Downloading bytes from chunk position:{ positionInChunk }. Chunk.StartIndex:{chunk.StartIndex}, Chunk.EndIndex:{chunk.EndIndex}");

            totalBytesRead = chunk.Chunk.DownloadRangeToByteArray(buffer, offset, positionInChunk, count);
            System.Console.WriteLine($"Bytes actually read from current chunk stream:{ totalBytesRead }");

            //increase the overall position by the amount of bytes read
            _position += totalBytesRead;


            return totalBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long _originalPosition = _position;

            if(offset < 0)
            {
                //If offset is negative, the new position is required to precede the position specified by origin by the number of bytes specified by offset.
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _position = offset;
                        break;
                    case SeekOrigin.Current:
                        _position = _position + offset;
                        break;
                    case SeekOrigin.End:
                        _position = (_length - 1) + offset;
                        break;
                }
            } else if(offset == 0)
            {
                //If offset is zero (0), the new position is required to be the position specified by origin. 
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _position = 0;
                        break;
                    case SeekOrigin.Current:
                        //don't move the position
                        break;
                    case SeekOrigin.End:
                        _position = _length - 1;
                        break;
                }
            }
            else
            {
                //If offset is positive, the new position is required to follow the position specified by origin by the number of bytes specified by offset.
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        _position = offset;
                        break;
                    case SeekOrigin.Current:
                        _position += offset;
                        break;
                    case SeekOrigin.End:
                        _position = (_length - 1) + offset;
                        break;
                }
            }

            //Console.WriteLine($"Seek - original position:{ _originalPosition }, seek origin:{ origin.ToString() }, offset:{ offset }, new position:{ _position }");

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        internal class ChunkItem
        {
            /// <summary>
            /// Gets or sets the 0 based index of the start of the chunk relative to all the chunks.
            /// </summary>
            public long StartIndex { get; set; }
            /// <summary>
            /// Gets or sets the 0 based index of the end of the chunk relative to all the chunks.
            /// </summary>
            public long EndIndex { get; set; }

            public ICloudBlob Chunk { get; set; }
        }
    }
}
