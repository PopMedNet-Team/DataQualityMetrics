using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class UserChangeLog
    {
        static readonly Guid UserChangeEventID = new Guid("B7640001-7247-49B8-A818-A22200CCEAF7");

        public UserChangeLog()
        {
            EventID = UserChangeEventID;
        }
        public Guid UserID { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public Guid EventID { get; set; }

        public EntityState Reason { get; set; }

        public string Description { get; set; }

        public Guid UserChangedID { get; set; }
        public virtual User UserChanged { get; set; }
    }

    public enum EntityState
    {
        Detached = 1,
        Unchanged = 2, 
        Added = 4,
        Deleted = 8,
        Modified = 16
    }
}
