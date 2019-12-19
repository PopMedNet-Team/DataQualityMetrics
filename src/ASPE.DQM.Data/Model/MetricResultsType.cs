using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class MetricResultsType : EntityWithID
    {
        public MetricResultsType()
        {

        }

        public string Value { get; set; }

        public virtual ICollection<Metric> Metrics { get; set; }
    }
}
