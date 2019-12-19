using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ASPE.DQM.Sync
{


    /// <summary>
    /// A log item for each entity synchronized by a job.
    /// </summary>
    public class SyncLogItem
    {

        public SyncLogItem()
        {
            ID = Model.EntityWithID.NewGuid();
            Action = SyncAction.Modified;
            Timestamp = DateTimeOffset.Now;
        }

        /// <summary>
        /// The ID of the sync log item.
        /// </summary>
        [Key]
        public Guid ID { get; set; }
        /// <summary>
        /// The DateTime the entity was synchronized.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
        /// <summary>
        /// A description of the item synchronized.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The ID of the entity that was synchronized.
        /// </summary>
        public Guid ItemID { get; set; }
        /// <summary>
        /// The synchronization action taken for the entity.
        /// </summary>
        public SyncAction Action { get; set; }
        /// <summary>
        /// The ID of the synchronization job the item belongs to.
        /// </summary>
        public Guid JobID { get; set; }
        /// <summary>
        /// Gets or sets the synchronization job the item belongs to.
        /// </summary>
        public virtual SyncJob Job { get; set; }
    }

    /// <summary>
    /// Synchronization action taken for each entity.
    /// </summary>
    public enum SyncAction
    {
        /// <summary>
        /// The entity was deleted.
        /// </summary>
        Deleted = 2,
        /// <summary>
        /// The entity was modified.
        /// </summary>
        Modified = 3,
        /// <summary>
        /// The entity was added.
        /// </summary>
        Added = 4
    }
}
