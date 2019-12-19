using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PMN.Sync
{
    public interface ISynchronizer : IDisposable
    {
        Guid EveryoneSecurityGroupID { get; }

        IEnumerable<Guid> SecurityGroupIDs { get; }

        TimeSpan SyncInterval { get; }

        int MaxErrorCount { get; }

        double MaxErrorsWithinHours { get; }

        Guid SyncID { get; }

        Task StartJobAsync();

        Task UpdateUsersAsync(IEnumerable<Data.UserSyncItemDTO> users, List<Guid> deletedUserIDs);

        Task<List<Guid>> DisableUsersAsync(IEnumerable<Data.UserSyncItemDTO> usersToDisable, List<Guid> deletedUserIDs);

        Task StopJobAsync();
    }
}
