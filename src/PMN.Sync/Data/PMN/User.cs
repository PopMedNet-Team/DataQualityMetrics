using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class User
    {
        public Guid ID { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public bool Active { get; set; }

        public bool Deleted { get; set; }

        public DateTime? DeactivatedOn { get; set; }

        public Guid? OrganizationID { get; set; }

        public Organization Organization { get; set; }

        public virtual ICollection<SecurityGroupUser> SecurityGroups { get; set; }

        public virtual ICollection<UserChangeLog> UserChangeLogs { get; set; }

        public virtual ICollection<ProfileUpdatedLog> ProfileUpdatedLogs { get; set; }

        public virtual ICollection<UserRegistrationChangedLog> RegistrationChangedLogs { get; set; }
    }
}
