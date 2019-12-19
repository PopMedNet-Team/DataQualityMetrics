using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class MetricStatusItem : EntityWithID
    {
        public MetricStatusItem()
        {
            CreateOn = DateTime.UtcNow;
        }

        public Guid MetricID { get; set; }

        public virtual Metric Metric { get; set; }

        public Guid UserID { get; set; }

        public virtual User User { get; set; }

        public Guid? PreviousMetricStatusID { get; set; }

        public virtual MetricStatusItem PreviousMetricStatus { get; set; }

        public DateTime CreateOn { get; set; }

        public Guid MetricStatusID { get; set; }

        public virtual MetricStatus MetricStatus { get; set; }

        public string Note { get; set; }
    }
}
