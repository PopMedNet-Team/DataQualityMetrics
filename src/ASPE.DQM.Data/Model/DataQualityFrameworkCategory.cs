using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class DataQualityFrameworkCategory : EntityWithID
    {
        public DataQualityFrameworkCategory()
        {

        }

        public string Title { get; set; }

        public string SubCategory { get; set; }

        public virtual ICollection<Metric_DataQualityFrameworkCategory> Metrics { get; set; }
    }
}
