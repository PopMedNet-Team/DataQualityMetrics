using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Models
{
    public class MeasureSubmissionViewModel
    {
        public Guid? MetricID { get; set; }

        public Guid? OrganizationID { get; set; }

        public string Organization { get; set; }

        public Guid? DataSourceID { get; set; }

        public string DataSource { get; set; }

        public DateTime? RunDate { get; set; }

        public string Network { get; set; }

        public string CommonDataModel { get; set; }

        public string DatabaseSystem { get; set; }

        public DateTime? DateRangeStart { get; set; }

        public DateTime? DateRangeEnd { get; set; }

        public string ResultsType { get; set; }

        public string CommonDataModelVersion { get; set; }

        public string ResultsDelimiter { get; set; }

        public string SupportingResources { get; set; }

        public IEnumerable<MeasurementSubmissionViewModel> Measures { get; set; }
    }

    public class MeasurementSubmissionViewModel
    {
        public string RawValue { get; set; }

        public string Definition { get; set; }

        public float? Measure { get; set; }

        public float? Total { get; set; }

        public bool IsNotNull()
        {
            return !string.IsNullOrEmpty(RawValue) || !string.IsNullOrEmpty(Definition) || Measure.HasValue || Total.HasValue;
        }
    }
}
