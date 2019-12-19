using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    [Flags]
    public enum MetricStatusAccess
    {
        None = 0,
        Author = 1,
        SystemAdministrator = 2,
        AuthenticatedUser = 4,
        Public = 8
    }

    public class MetricStatus : EntityWithID
    {

        public MetricStatus()
        {
            Order = 0;
            Access = MetricStatusAccess.None;
            AllowEdit = false;
        }

        public string Title { get; set; }

        public MetricStatusAccess Access { get; set; }

        public int Order { get; set; }

        public bool AllowEdit { get; set; }


        public static readonly Guid DraftID = new Guid("AF5892EA-807C-4F1D-9989-AA4F00B9CB96");
        public static readonly Guid SubmittedID = new Guid("91BFF71D-6E3B-4D5A-8947-AA4F00B9CB96");
        public static readonly Guid InReviewID = new Guid("E7D3591C-D912-42C6-88E2-AA4F00B9CB96");
        public static readonly Guid PublishedID = new Guid("3CE548A3-4E91-4FE0-9D70-AA4F00B9CB96");
        public static readonly Guid PublishedRequiresAuthenticationID = new Guid("A56E66BA-6088-49DF-9247-AA4F00B9CB96");
        public static readonly Guid RejectedID = new Guid("546E8D36-4979-449A-B730-AA4F00B9CB96");
        public static readonly Guid InactiveID = new Guid("AC70E2A2-9C22-4D1E-B378-AA4F00B9CB96");
        public static readonly Guid DeletedID = new Guid("0B930582-060A-4FE8-AB50-AA4F00B9CB96");

    }
}
