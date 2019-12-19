using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASPE.DQM.Model
{
    public class Metric : EntityWithID
    {
        public Metric()
        {
            CreatedOn = DateTime.UtcNow;
            ModifiedOn = DateTime.UtcNow;
            ServiceDeskUrl = string.Empty;
            FrameworkCategories = new HashSet<Metric_DataQualityFrameworkCategory>();
            Domains = new HashSet<Metric_Domain>();
            Statuses = new HashSet<MetricStatusItem>();
            Measurements = new HashSet<MeasurementMeta>();
        }

        public Guid AuthorID { get; set; }

        public virtual User Author { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Justification { get; set; }

        public string ExpectedResults { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string ServiceDeskUrl { get; set; }

        public Guid ResultsTypeID { get; set; }

        public virtual MetricResultsType ResultsType { get; set; }

        public virtual ICollection<Metric_DataQualityFrameworkCategory> FrameworkCategories { get; private set; }

        public virtual ICollection<Metric_Domain> Domains { get; private set; }

        public virtual ICollection<MetricStatusItem> Statuses { get; private set; }

        public virtual ICollection<MeasurementMeta> Measurements { get; set; }

        public void AddFrameworkCategories(params DataQualityFrameworkCategory[] categories)
        {
            foreach(var category in categories)
            {
                FrameworkCategories.Add(new Metric_DataQualityFrameworkCategory { Metric = this, DataQualityFrameworkCategory = category, DataQualityFrameworkCategoryID = category.ID });
            }
        }

        public void AddFrameworkCategories(params Guid[] categoryIDs)
        {
            foreach (var id in categoryIDs)
            {
                FrameworkCategories.Add(new Metric_DataQualityFrameworkCategory { Metric = this, DataQualityFrameworkCategoryID = id });
            }
        }

        public void AddDomains(params Domain[] domains)
        {
            foreach (var domain in domains)
            {
                Domains.Add(new Metric_Domain { Metric = this, Domain = domain, DomainID = domain.ID });
            }
        }

        public void AddDomains(params Guid[] domainIDs)
        {
            foreach (var id in domainIDs)
            {
                Domains.Add(new Metric_Domain { Metric = this, DomainID = id });
            }
        }



    }

    public class Metric_DataQualityFrameworkCategory
    {
        public Metric_DataQualityFrameworkCategory() { }

        public Guid MetricID { get; set; }

        public virtual Metric Metric { get; set; }

        public Guid DataQualityFrameworkCategoryID { get; set; }

        public virtual DataQualityFrameworkCategory DataQualityFrameworkCategory { get; set; }
    }

    public class Metric_Domain
    {
        public Metric_Domain() { }

        public Guid MetricID { get; set; }

        public virtual Metric Metric { get; set; }

        public Guid DomainID { get; set; }

        public virtual Domain Domain { get; set; }
    }
}
