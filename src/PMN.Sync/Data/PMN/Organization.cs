using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class Organization
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public string Acronym { get; set; }

        public bool Deleted { get; set; }

        public ICollection<User> Users { get; set; }
    }
}
