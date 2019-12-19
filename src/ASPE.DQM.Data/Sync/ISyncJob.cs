using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Sync
{
    /// <summary>
    /// The details of a synchronization job.
    /// </summary>
    public interface ISyncJob
    {
        /// <summary>
        /// Gets the ID of the job.
        /// </summary>
        Guid ID { get; }
        /// <summary>
        /// Gets the start date time of the job.
        /// </summary>
        DateTimeOffset Start { get; }
        /// <summary>
        /// Gets the date time the job stopped.
        /// </summary>
        DateTimeOffset? End { get; }
        /// <summary>
        /// Gets the status of the job.
        /// </summary>
        SyncJobStatus Status { get; }
        /// <summary>
        /// Gets any message associated with the job.
        /// </summary>
        string Message { get; }
    }
}
