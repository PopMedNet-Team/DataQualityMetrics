using System;

namespace ASPE.DQM.Models
{
    public class MetricDetailsViewModel
    {
        /// <summary>
        /// Gets or sets the ID of the metric.
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// Gets or sets the title of the metric.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Gets the title of the metric limited to a maximum of 50 characters.
        /// </summary>
        public string TitleTrimmed
        {
            get { return Utils.StringEx.Ellipse(Title, 50, "..."); }
        }

        public bool AllowView { get; set; }

        public bool AllowEdit { get; set; }

        public bool AllowCopy { get; set; }

        public bool IsSystemAdministrator { get; set; }

        public Guid CurrentStatusID { get; set; }

        public bool IsAuthor { get; set; }
    }
}
