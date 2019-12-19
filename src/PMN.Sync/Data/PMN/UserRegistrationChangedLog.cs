using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class UserRegistrationChangedLog
    {
        static readonly Guid UserRegistrationChangedEventID = new Guid("76B10001-2B49-453C-A8E1-A22200CC9356");

        public UserRegistrationChangedLog()
        {
            EventID = UserRegistrationChangedEventID;
        }

        public Guid UserID { get; set; }
        public DateTimeOffset TimeStamp { get; set; }

        public Guid EventID { get; set; }

        public string Description { get; set; }

        public Guid RegisteredUserID { get; set; }
        public virtual User RegisteredUser { get; set; }
    }
}
