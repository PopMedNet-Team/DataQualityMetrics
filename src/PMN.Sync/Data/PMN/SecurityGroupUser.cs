using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    [Table("SecurityGroupUsers")]
    public class SecurityGroupUser
    {
        public Guid SecurityGroupID { get; set; }

        public Guid UserID { get; set; }

        public virtual User User { get; set; }

        public bool Overriden { get; set; }
    }
}
