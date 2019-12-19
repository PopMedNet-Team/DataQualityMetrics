using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class Domain : EntityWithID
    {

        public Domain() { }

        public string Title { get; set; }

        public virtual ICollection<Metric_Domain> Metrics { get; set; }
    }
}
