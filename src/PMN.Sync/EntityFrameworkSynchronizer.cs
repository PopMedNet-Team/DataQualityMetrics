using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using PMN.Sync.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PMN.Sync
{
    public class EntityFrameworkSynchronizer : ISynchronizer
    {
        readonly ILogger<EntityFrameworkSynchronizer> Logger;
        readonly IConfiguration Config;
        readonly Dictionary<Guid, string> RolesToClaimsLookup;
        readonly UserManager<ASPE.DQM.Identity.IdentityUser> UserManager;
        readonly ASPE.DQM.Sync.SyncDataContext _db;
        readonly ASPE.DQM.Model.ModelDataContext _modelDb;

        public EntityFrameworkSynchronizer(IConfiguration config, ILogger<EntityFrameworkSynchronizer> logger, ASPE.DQM.Sync.SyncDataContext db, ASPE.DQM.Model.ModelDataContext modelDb, UserManager<ASPE.DQM.Identity.IdentityUser> userManager)
        {
            Logger = logger;
            Config = config;
            UserManager = userManager;
            _db = db;
            _modelDb = modelDb;

            var syncConfig = config.GetSection("Sync");
            SyncInterval = TimeSpan.FromSeconds(syncConfig.GetValue<int>("Interval"));
            MaxErrorCount = syncConfig.GetValue<int>("MaxErrors");
            MaxErrorsWithinHours = syncConfig.GetValue<double>("MaxErrorsWithinHours");

            SyncID = Guid.Empty;

            var rolesConfig = config.GetSection("AuthorizationRoles");
            EveryoneSecurityGroupID = Guid.Parse(rolesConfig["Everyone"]);

            SecurityGroupIDs = rolesConfig.AsEnumerable().Skip(1).Where(c => !string.IsNullOrEmpty(c.Value)).Select(c => Guid.Parse(c.Value)).ToArray();

            RolesToClaimsLookup = rolesConfig.AsEnumerable().Skip(1).ToDictionary(c => Guid.Parse(c.Value), c => c.Key.Split(':')[1]);

            
        }

        public Guid EveryoneSecurityGroupID { get; }

        public IEnumerable<Guid> SecurityGroupIDs { get; }

        public TimeSpan SyncInterval { get; }

        public int MaxErrorCount { get; }

        public double MaxErrorsWithinHours { get; }

        public Guid SyncID { get; private set; }

        public async Task StartJobAsync()
        {
            var job = new ASPE.DQM.Sync.SyncJob();
            _db.SyncJobs.Add(job);
            await _db.SaveChangesAsync();

            SyncID = job.ID;
        }

        public async Task UpdateUsersAsync(IEnumerable<UserSyncItemDTO> users, List<Guid> deletedUserIDs)
        {
            var syncJob = await _db.FindAsync<ASPE.DQM.Sync.SyncJob>(SyncID);
            if (syncJob == null)
            {
                throw new Exception("Invalid synchronization job identifier.");
            }

            if (syncJob.Status != ASPE.DQM.Sync.SyncJobStatus.Running)
            {
                throw new Exception("Invalid synchronization job status: " + syncJob.Status.ToString());
            }

            //populate the claims from the assigned security groups for each user
            users = users.Select(u => {
                var claims = new List<KeyValuePair<string, string>>();
                claims.AddRange(u.SecurityGroups.Where(sg => sg != EveryoneSecurityGroupID).ToArray().Select(sg => new KeyValuePair<string, string>(RolesToClaimsLookup[sg], RolesToClaimsLookup[sg])).ToArray());

                if (!string.IsNullOrEmpty(u.FirstName))
                    claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.FirstName_Key, u.FirstName));

                if (!string.IsNullOrEmpty(u.LastName))
                    claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.LastName_Key, u.LastName));

                if (!string.IsNullOrEmpty(u.Phone))
                    claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.Phone_Key, u.Phone));

                claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.Organization_Key, u.Organization));

                u.Claims = claims;
                return u;
            }).ToArray();

            var result = await syncJob.SyncUsersAsync(UserManager, users);

            if (_db.ChangeTracker.HasChanges())
            {
                await _db.SaveChangesAsync();
            }

            //update the User in models
            var modelUsers = _modelDb.Users.Where(u => users.Any(d => d.ID == u.ID)).ToArray();
            foreach(var updateUser in users.Where(u => modelUsers.Any(mu => mu.ID == u.ID)))
            {
                var m_user = modelUsers.First(m => m.ID == updateUser.ID);
                m_user.FirstName = updateUser.FirstName;
                m_user.LastName = updateUser.LastName;
                m_user.Organization = updateUser.Organization;
                m_user.PhoneNumber = updateUser.Phone;
                m_user.UserName = updateUser.UserName;
                m_user.Email = updateUser.Email;
            }

            if (_modelDb.ChangeTracker.HasChanges())
            {
                await _modelDb.SaveChangesAsync();
            }

            //add any missing model users
            using(var cmd = _modelDb.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = @"SELECT u.Id,
ISNULL(u.UserName, '') AS UserName,
ISNULL(u.Email, '') AS Email,
ISNULL(u.PhoneNumber, '') AS PhoneNumber,
ISNULL((SELECT TOP 1 cl.ClaimValue FROM AspNetUserClaims cl WHERE cl.UserId = u.Id AND cl.ClaimType = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'), '') AS FirstName,
ISNULL((SELECT TOP 1 cl.ClaimValue FROM AspNetUserClaims cl WHERE cl.UserId = u.Id AND cl.ClaimType = 'USER.ORGANIZATION'), '') AS Organization,
ISNULL((SELECT TOP 1 cl.ClaimValue FROM AspNetUserClaims cl WHERE cl.UserId = u.Id AND cl.ClaimType = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'), '') AS LastName
FROM AspNetUsers u WHERE NOT EXISTS(SELECT NULL FROM Users mu WHERE mu.ID = u.Id)";

                await _modelDb.Database.OpenConnectionAsync();

                using(var reader = await cmd.ExecuteReaderAsync())
                {
                    while(await reader.ReadAsync())
                    {
                        _modelDb.Users.Add(new ASPE.DQM.Model.User {
                            ID = reader.GetGuid(0),
                            UserName = reader.GetString(1),
                            Email = reader.GetString(2),
                            PhoneNumber = reader.GetString(3),
                            FirstName = reader.GetString(4),
                            Organization = reader.GetString(5),
                            LastName = reader.GetString(6)
                        });
                    }
                }

            }

            if (_modelDb.ChangeTracker.HasChanges())
            {
                await _modelDb.SaveChangesAsync();
            }


            if (result != null && result.Any())
            {
                throw new Exception(string.Join(Environment.NewLine, result.ToArray()));
            }
        }

        public string TranslateSecurityGroupToClaim(Guid securityGroupID)
        {
            return RolesToClaimsLookup.GetValueOrDefault(securityGroupID);
        }

        public async Task<List<Guid>> DisableUsersAsync(IEnumerable<UserSyncItemDTO> usersToDisable, List<Guid> deletedUserIDs)
        {
            var syncJob = await _db.FindAsync<ASPE.DQM.Sync.SyncJob>(SyncID);
            if (syncJob == null)
            {
                throw new Exception("Invalid synchronization job identifier.");
            }

            if (syncJob.Status != ASPE.DQM.Sync.SyncJobStatus.Running)
            {
                throw new Exception("Invalid synchronization job status: " + syncJob.Status.ToString());
            }

            var result = await syncJob.DisableUsersAsync(UserManager, usersToDisable.Where(u => !deletedUserIDs.Contains(u.ID)).ToArray());

            await _db.SaveChangesAsync();

            if (result != null && result.Any())
            {
                throw new Exception(string.Join(Environment.NewLine, result.ToArray()));
            }

            //update the already deleted users list
            return new List<Guid>(usersToDisable.Select(u => u.ID).ToArray());
        }

        public async Task StopJobAsync()
        {
            var job = await _db.FindAsync<ASPE.DQM.Sync.SyncJob>(SyncID);
            if (job != null && job.Status == ASPE.DQM.Sync.SyncJobStatus.Running)
            {
                job.Stop();
                await _db.SaveChangesAsync();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _db.Dispose();
                    UserManager.Dispose();
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpSynchronizer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
