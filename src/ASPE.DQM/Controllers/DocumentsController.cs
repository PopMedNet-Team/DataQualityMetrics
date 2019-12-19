using ASPE.DQM.Files;
using ASPE.DQM.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : Controller
    {
        readonly ModelDataContext _modelDB;
        readonly IConfiguration _config;
        readonly IFileService _fileService;

        public DocumentsController(ModelDataContext modelDB, IConfiguration config, IServiceProvider serviceProvider)
        {
            _modelDB = modelDB;
            _config = config;
            _fileService = (IFileService)serviceProvider.GetRequiredService(Type.GetType(config["Files:Type"]));
        }        

        /// <summary>
        /// Endpoint to List all the Documents associated to a specific Item.
        /// </summary>
        /// <param name="id">The Identifier of the Item</param>
        /// <returns>Returns a Listing of <see cref="DocumentDTO"/></returns>
        [AllowAnonymous, HttpGet("List/{id}")]
        public async Task<IActionResult> List(Guid id)
        {
            var docs = await _modelDB.Documents.Where(x => x.ItemID == id).Include(x => x.UploadedBy)
                .Select(doc => new DocumentDTO
                {
                    ID = doc.ID,
                    Name = doc.Name,
                    MimeType = doc.MimeType,
                    CreatedOn = doc.CreatedOn,
                    Size = doc.Length,
                    ItemID = doc.ItemID,
                    UserName = doc.UploadedBy.UserName,
                    FirstName = doc.UploadedBy.FirstName,
                    LastName = doc.UploadedBy.LastName
                }).ToArrayAsync();

            return Json(docs);
        }

        /// <summary>
        /// Endpoint for uploading a file.
        /// </summary>
        /// <param name="files">The Files that are uploaded.</param>
        /// <param name="metaData">The Metadata sent by the Upload Control.</param>
        /// <returns>Returns the Result of the upload <see cref="UploadChuckResult"/></returns>
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload(IEnumerable<IFormFile> files, [FromForm] string metaData)
        {
            if (!Request.ContentType.Contains("multipart/form-data"))
            {
                return BadRequest("Content must be mime multipart.");
            }

            var user = await _modelDB.Users.FindAsync(Guid.Parse(User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()));

            ChunkMetaData metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkMetaData>(Request.Form["metadata"]);

            await _fileService.WriteToStreamAsync(files.FirstOrDefault(), metadata);

            if (!metadata.IsFinalChunck)
            {
                return Json(new UploadChuckResult { fileUid = metadata.UploadUid, uploaded = metadata.IsFinalChunck });
            }

            Guid itemID;
            if (!Guid.TryParse(Request.Form["ItemID"], out itemID))
            {
                return BadRequest("The ID of the owning entity for the document was not specified.");
            }

            var document = await _fileService.SaveDocumentToDatabaseAsync(_modelDB, metadata, itemID, user.ID);
            await _fileService.FinalizeUploadAsync(document, metadata);

            return Json(new UploadChuckResult { fileUid = metadata.UploadUid, uploaded = metadata.IsFinalChunck });
        }

        /// <summary>
        /// Endpoint for Downloading a specific document.
        /// </summary>
        /// <param name="id">The Identifier of the Document.</param>
        /// <returns>Returns a Stream of the File.</returns>
        [HttpGet("Download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            return await _fileService.ReturnDownloadActionResultAsync(id, Response);
        }

        /// <summary>
        /// Endpoint for Deleting a document.
        /// </summary>
        /// <param name="id">The Identifier of the Document.</param>
        /// <returns></returns>
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var doc = await _modelDB.Documents.Where(x => x.ID == id).FirstOrDefaultAsync();

            _modelDB.Remove(doc);

            await _modelDB.SaveChangesAsync();

            await _fileService.DeleteFileChunksAsync(id);

            return Ok();
        }

        public class DocumentDTO
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string MimeType { get; set; }
            public DateTime CreatedOn { get; set; }
            public long Size { get; set; }
            public Guid ItemID { get; set; }
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}