using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ASPE.DQM.Model
{
    public class User : EntityWithID
    {
        public User() { }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Organization { get; set; }

        public virtual ICollection<Metric> AuthoredMetrics { get; set; }

        public virtual ICollection<Document> Documents { get; set; }

        public virtual ICollection<MeasurementMeta> Measurements { get; set; }

        public virtual ICollection<UserVisualizationFavorite> FavoriteVisualizations { get; set; }

        public virtual ICollection<UserMetricFavorite> FavoriteMetrics { get; set; }

    }
    public class UserVisualizationFavorite
    {

        public UserVisualizationFavorite()
        {
            CreatedOn = DateTime.UtcNow;
        }

        public Guid UserID { get; set; }

        public virtual User User { get; set; }

        public Guid VisualizationID { get; set; }

        public virtual Visualization Visualization { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    public class UserMetricFavorite
    {

        public UserMetricFavorite()
        {
            CreatedOn = DateTime.UtcNow;
        }

        public Guid UserID { get; set; }

        public virtual User User { get; set; }

        public Guid MetricID { get; set; }

        public virtual Metric Metric { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
