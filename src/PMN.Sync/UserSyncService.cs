using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace PMN.Sync
{

    public class UserSyncService : ISyncService
    {
        readonly ILogger<UserSyncService> Logger;
        readonly Data.PMN.DataContext Db;
        readonly IConfiguration Config;
        
        readonly ISynchronizer Synchronizer;

        DateTime _lastUserQuery = DateTime.UtcNow.AddYears(-100);
        List<Guid> _deletedUserIDs = new List<Guid>();

        public bool Running { get; private set; }

        int _errorCount = 0;
        DateTime? _lastError = null;

        public UserSyncService(Data.PMN.DataContext db, IConfiguration config, ILogger<UserSyncService> logger, ISynchronizer synchronizer)
        {
            Running = false;
            Db = db;
            Logger = logger;
            Config = config;
            Synchronizer = synchronizer;
        }

        public async Task Run() {

            Running = true;

            DateTime syncStart;
            while (Running)
            {
                syncStart = DateTime.UtcNow;
                try
                {
                    await SyncUsers();
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "An error occured while synchronizing!");
                    _errorCount++;
                    _lastError = DateTime.UtcNow;
                }

                if(DateTime.UtcNow - _lastError > TimeSpan.FromHours(Synchronizer.MaxErrorsWithinHours))
                {
                    _errorCount = 0;
                }

                if(_errorCount >= Synchronizer.MaxErrorCount)
                {
                    Running = false;
                    throw new ApplicationException("Too many errors have occured in the last hour, stopping synching!!");
                }

                //if the elaspsed time is shorter than the sync interval sleep
                    var timeout = Synchronizer.SyncInterval - (DateTime.UtcNow - syncStart);
                if (timeout.TotalMilliseconds > 0)
                {
                    System.Threading.Thread.Sleep(timeout);
                }
            }


        }

        async Task SyncUsers()
        {
            //get the last sync date, if not exists select all user's. Else select only the users that have changes.
            var users = await (from u in Db.Users
                               let lastQuery = _lastUserQuery
                               where u.Active && u.Deleted == false
                               && u.SecurityGroups.Any(sg => sg.SecurityGroupID == Synchronizer.EveryoneSecurityGroupID)
                               && (
                               Db.UserChangeLogs.Where(l => l.TimeStamp >= lastQuery && l.UserChangedID == u.ID).Any()
                               || Db.UserProfileUpdatedLogs.Where(l => l.TimeStamp >= lastQuery && l.UserChangedID == u.ID).Any()
                               || Db.UserRegistrationChangedLogs.Where(l => l.TimeStamp >= lastQuery && l.RegisteredUserID == u.ID).Any()
                               )
                               select new Data.UserSyncItemDTO
                               {
                                   ID = u.ID,
                                   UserName = u.UserName,
                                   FirstName = u.FirstName,
                                   LastName = u.LastName,
                                   Phone = u.Phone,
                                   Email = u.Email,
                                   PasswordHash = u.PasswordHash,
                                   SecurityGroups = u.SecurityGroups.Where(sg => Synchronizer.SecurityGroupIDs.Contains(sg.SecurityGroupID)).Select(sg => sg.SecurityGroupID),
                                   Organization = u.Organization.Name
                               }).ToArrayAsync();

            var usersToDisable = await (from u in Db.Users
                                        where
                                        //users who have been disabled
                                        u.Active == false 
                                        //users that have been deleted
                                        || u.Deleted
                                        //users who do not contain the EveryoneSecurity group
                                        || !u.SecurityGroups.Any(sgu => sgu.SecurityGroupID == Synchronizer.EveryoneSecurityGroupID)
                                        select new Data.UserSyncItemDTO
                                        {
                                            ID = u.ID,
                                            UserName = u.UserName
                                        }).ToArrayAsync();

            _lastUserQuery = DateTime.UtcNow;

            //remove the previously disabled users from the sync content since they have already been pushed
            var disabledUsersCount = usersToDisable.Where(u => !_deletedUserIDs.Contains(u.ID)).Count();

            if ((users.Length == 0 && disabledUsersCount == 0) || !Running)
            {
                Logger.LogInformation("No users to synchronize with DQM.");
                return;
            }

            try
            {
                //start the sync job
                await Synchronizer.StartJobAsync();

                if (!Running)
                {
                    return;
                }

                //push the actuve PMN users to DQM
                if (users.Any())
                {
                    Logger.LogDebug($"SyncJob:{ Synchronizer.SyncID } - { users.Length } users to ADD OR UPDATE since the last sync at { _lastUserQuery.ToLocalTime() }");

                    await Synchronizer.UpdateUsersAsync(users, _deletedUserIDs);

                    if (!Running)
                    {
                        return;
                    }
                }

                //disable the deleted/de-activated users                
                if (disabledUsersCount > 0)
                {
                    Logger.LogDebug($"SyncJob:{ Synchronizer.SyncID } - { disabledUsersCount } users to DISABLE since the last sync at { _lastUserQuery.ToLocalTime() }");

                    _deletedUserIDs = await Synchronizer.DisableUsersAsync(usersToDisable, _deletedUserIDs);
                }

            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, $"Error synchronizing users, SyncJob:{ Synchronizer.SyncID }.");
            }
            finally
            {
                if (Synchronizer.SyncID != Guid.Empty)
                {
                    //stop the sync
                    await Synchronizer.StopJobAsync();

                }
            }
        }

        public async Task Stop() {
            await Task.Run(() => Running = false);
        }
    }

    class StartSyncJobResponse
    {
        public Guid id { get; set; }
    }
}
