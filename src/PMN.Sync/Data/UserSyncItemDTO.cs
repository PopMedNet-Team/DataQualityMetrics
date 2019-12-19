using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data
{
    public class UserSyncItemDTO : ASPE.DQM.Sync.IUserSyncItem
    {

        public UserSyncItemDTO() { }

        public Guid ID { get; set; }

        [JsonIgnore]
        public string FirstName { get; set; }

        [JsonIgnore]
        public string LastName { get; set; }

        public string UserName { get; set; }

        [JsonIgnore]
        public string Phone { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public IEnumerable<KeyValuePair<string,string>> Claims { get; set; }

        [JsonIgnore]
        public IEnumerable<Guid> SecurityGroups { get; set; }

        [JsonIgnore]
        public string Organization { get; set; }

        public string GetID()
        {
            return ID.ToString("D").ToLower();
        }
    }
}
