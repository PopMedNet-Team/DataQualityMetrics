using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ASPE.DQM.Sync
{
    public interface IUserSyncItem
    {
        Guid ID { get; }

        string UserName { get; }

        string Email { get; }

        string PasswordHash { get; }

        IEnumerable<KeyValuePair<string,string>> Claims { get; set; }

        string GetID();
    }

    public interface IUserSyncSet
    {
        Guid JobID { get; }
        IEnumerable<IUserSyncItem> Users { get; }
    }

    public class UserSyncItem : IUserSyncItem
    {
        public Guid ID { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public IEnumerable<KeyValuePair<string,string>> Claims { get; set; }

        public string GetID()
        {
            return ID.ToString("D").ToLower();
        }
    }

    public class UserSyncSet : IUserSyncSet
    {
        public Guid JobID { get; set; }

        public IEnumerable<UserSyncItem> Users { get; set; }

        IEnumerable<IUserSyncItem> IUserSyncSet.Users {  get { return Users.Cast<IUserSyncItem>();  } }
    }
}
