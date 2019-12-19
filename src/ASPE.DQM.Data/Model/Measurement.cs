using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class MeasurementMeta : EntityWithID
    {

        public MeasurementMeta() : base()
        {
            this.SubmittedOn = DateTime.UtcNow;
            this.Measurements = new HashSet<Measurement>();
        }

        public Guid MetricID { get; set; }

        public virtual Metric Metric { get; set; }

        public Guid? OrganizationID { get; set; }

        public string Organization { get; set; }

        public Guid? DataSourceID { get; set; }

        public string DataSource { get; set; }

        public DateTime RunDate { get; set; }

        public string Network { get; set; }

        public string CommonDataModel { get; set; }

        public string DatabaseSystem { get; set; }

        public DateTime DateRangeStart { get; set; }

        public DateTime DateRangeEnd { get; set; }

        public Guid ResultsTypeID { get; set; }

        public virtual ICollection<Measurement> Measurements { get; set; }

        public Guid? SuspendedByID { get; set; }

        public virtual User SuspendedBy { get; set; }

        public DateTime? SuspendedOn { get; set; }

        public Guid SubmittedByID { get; set; }

        public User SubmittedBy { get; set; }

        public DateTime SubmittedOn { get; set; }

        public string CommonDataModelVersion { get; set; }

        public string ResultsDelimiter { get; set; }

        public string SupportingResources { get; set; }

    }

    public class Measurement : EntityWithID
    {
        public Guid MetadataID { get; set; }

        public MeasurementMeta Metadata { get; set; }

        public string RawValue { get; set; }

        public string Definition { get; set; }

        public float Measure { get; set; }

        public float? Total { get; set; }

    }
}
