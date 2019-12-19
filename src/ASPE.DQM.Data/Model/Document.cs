using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class Document : EntityWithID
    {
        public Document()
        {
            this.CreatedOn = DateTime.UtcNow;
            this.MajorVersion = 1;
            this.MinorVersion = 0;
            this.BuildVersion = 0;
            this.RevisionVersion = 0;
        }

        /// <summary>
        /// Gets or Sets the name of the document.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the filename of the document.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or Sets if the document is viewable (has a visualizer).
        /// </summary>
        public bool Viewable { get; set; }

        /// <summary>
        /// Gets or Sets the mime type of the document.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the document kind.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets the created on date.
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the last time the document content was modified.
        /// </summary>
        public DateTime? ContentModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the first time the document content was created.
        /// </summary>
        public DateTime? ContentCreatedOn { get; set; }

        /// <summary>
        /// Gets or set the length of the document in bytes.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or set the ID of the item the document is associated with, ie. Request, Response, Task, etc...
        /// </summary>
        public Guid ItemID { get; set; }

        /// <summary>
        /// Gets or set the description of the document.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ID of the document the current document is a revision of.
        /// </summary>
        public Guid? ParentDocumentID { get; set; }
        public virtual Document ParentDocument { get; set; }

        /// <summary>
        /// Gets or set the ID of the user who uploaded the document.
        /// </summary>
        public Guid? UploadedByID { get; set; }
        public virtual User UploadedBy { get; set; }

        /// <summary>
        /// Gets or sets an identifier that groups a set of revisions of a specific document.
        /// </summary>
        public Guid? RevisionSetID { get; set; }
        /// <summary>
        /// Gets or set a description of the revision.
        /// </summary>
        public string RevisionDescription { get; set; }
        /// <summary>
        /// Gets or sets the major version number. Version format: {major}.{minor}.{build}.{revision}
        /// </summary>
        public int MajorVersion { get; set; }
        /// <summary>
        /// Gets or sets the minor version number. Version format: {major}.{minor}.{build}.{revision}
        /// </summary>
        public int MinorVersion { get; set; }
        /// <summary>
        /// Gets or sets the build version number. Version format: {major}.{minor}.{build}.{revision}
        /// </summary>
        public int BuildVersion { get; set; }
        /// <summary>
        /// Gets or sets the revision version number. Version format: {major}.{minor}.{build}.{revision}
        /// </summary>
        public int RevisionVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ChunkCount { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }
}
