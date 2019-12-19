using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class ProfileUpdatedLog
    {
        static readonly Guid ProfileUpdatedEventID = new Guid("B7640001-7247-49B8-A818-A22200CCEAF7");

        public ProfileUpdatedLog()
        {
            EventID = ProfileUpdatedEventID;
        }

        public Guid UserID { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public Guid EventID { get; set; }

        public EntityState Reason { get; set; }

        public string Description { get; set; }

        public Guid UserChangedID { get; set; }
        public virtual User UserChanged { get; set; }
    }
}
