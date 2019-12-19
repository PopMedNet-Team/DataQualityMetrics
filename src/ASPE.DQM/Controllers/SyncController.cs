using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    public class SyncController : Controller
    {
        readonly Sync.SyncDataContext _db;
        readonly Model.ModelDataContext _modelDb;
        readonly IUserStore<Identity.IdentityUser> _userStore;
        readonly UserManager<Identity.IdentityUser> _userManager;
        readonly ILogger<SyncController> _logger;
        readonly IConfiguration _config;
        readonly string _syncKey;

        public SyncController(Sync.SyncDataContext db, Model.ModelDataContext modelDataContext, IUserStore<Identity.IdentityUser> userStore, UserManager<Identity.IdentityUser> userManager, ILogger<SyncController> logger, IConfiguration config)
        {
            _db = db;
            _modelDb = modelDataContext;
            _userStore = userStore;
            _userManager = userManager;
            _logger = logger;
            _config = config;

            _syncKey = config.GetSection("Sync").GetValue<string>("ServiceKey");

        }

        //authenticate using a token provided as a header of the request
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!string.IsNullOrEmpty(_syncKey))
            {
                var syncKey = context.HttpContext.Request.Headers["ServiceKey"];
                if (!string.Equals(_syncKey, syncKey, StringComparison.Ordinal))
                {
                    throw new System.Security.Authentication.InvalidCredentialException();
                }
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Returns the 1000 most recent synchronization job details in reverse chronological order (most recent first).
        /// </summary>
        /// <returns></returns>
        [HttpGet("recent")]
        public async Task<IEnumerable<Sync.ISyncJob>> Recent()
        {
            var result = await _db.SyncJobs.OrderByDescending(j => j.Start).Take(1000).ToArrayAsync();
            return result;
        }

        /// <summary>
        /// Returns details about the last or current synchronization job if no ID is specified, else the details about the job specified.
        /// </summary>
        /// <returns></returns>
        [HttpGet("get")]
        public async Task<Sync.ISyncJob> Get(Guid? id)
        {
            if (id.HasValue)
            {
                return await _db.FindAsync<Sync.SyncJob>(id.Value);
            }

            return await _db.SyncJobs.OrderByDescending(j => j.Start).FirstOrDefaultAsync();
        }
        
        /// <summary>
        /// Starts a new synchronization job.
        /// </summary>
        /// <returns>The details of a new synchronization job.</returns>
        [HttpPost("start")]
        public async Task<IActionResult> Start()
        {
            var job = new Sync.SyncJob();
            _db.SyncJobs.Add(job);
            await _db.SaveChangesAsync();

            return CreatedAtAction("Get", new { id = job.ID }, job);
        }

        /// <summary>
        /// Accepts a synchronization set of Users to update.
        /// </summary>
        /// <param name="syncSet"></param>
        [HttpPost("update-users")]
        public async Task<IActionResult> UpdateUsers([FromBody] Sync.UserSyncSet syncSet)
        {
            try
            {
                var syncJob = await _db.SyncJobs.FindAsync(syncSet.JobID);
                if (syncJob == null)
                {
                    return BadRequest(new[] { "Invalid synchronization job identifier." });
                }

                if (syncJob.Status != Sync.SyncJobStatus.Running)
                {
                    return BadRequest(new[] { "Invalid synchronization job status: " + syncJob.Status.ToString() });
                }

                var result = await syncJob.SyncUsersAsync(_userManager, syncSet.Users);

                if (_db.ChangeTracker.HasChanges())
                {
                    await _db.SaveChangesAsync();
                }

                var users = syncSet.Users;
                var modelUsers = _modelDb.Users.Where(u => users.Any(d => d.ID == u.ID)).ToArray();
                foreach (var updateUser in users.Where(u => modelUsers.Any(mu => mu.ID == u.ID)))
                {
                    var m_user = modelUsers.First(m => m.ID == updateUser.ID);
                    m_user.FirstName = updateUser.Claims.Where(cl => cl.Key == Identity.Claims.FirstName_Key).Select(cl => cl.Value).FirstOrDefault();
                    m_user.LastName = updateUser.Claims.Where(cl => cl.Key == Identity.Claims.LastName_Key).Select(cl => cl.Value).FirstOrDefault();
                    m_user.Organization = updateUser.Claims.Where(cl => cl.Key == Identity.Claims.Organization_Key).Select(cl => cl.Value).FirstOrDefault();
                    m_user.PhoneNumber = updateUser.Claims.Where(cl => cl.Key == Identity.Claims.Phone_Key).Select(cl => cl.Value).FirstOrDefault();
                    m_user.UserName = updateUser.UserName;
                    m_user.Email = updateUser.Email;
                }

                if (_modelDb.ChangeTracker.HasChanges())
                {
                    await _modelDb.SaveChangesAsync();
                }

                //add any missing model users
                using (var cmd = _modelDb.Database.GetDbConnection().CreateCommand())
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

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            _modelDb.Users.Add(new ASPE.DQM.Model.User
                            {
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
                    return BadRequest(result.ToArray());
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error updating users for sync job: { syncSet.JobID }");
                return BadRequest(new[] { "Error updating users for sync job." });
            }

            return Ok();
        }

        [HttpPost("disable-users")]
        public async Task<IActionResult> DisableUsers([FromBody] Sync.UserSyncSet syncSet)
        {
            try
            {
                var syncJob = await _db.SyncJobs.FindAsync(syncSet.JobID);
                if (syncJob == null)
                {
                    return BadRequest(new[] { "Invalid synchronization job identifier." });
                }

                if (syncJob.Status != Sync.SyncJobStatus.Running)
                {
                    return BadRequest(new[] { "Invalid synchronization job status: " + syncJob.Status.ToString() });
                }

                var result = await syncJob.DisableUsersAsync(_userManager, syncSet.Users);

                await _db.SaveChangesAsync();

                if (result != null && result.Any())
                {
                    return BadRequest(result.ToArray());
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error disabling users for sync job: { syncSet.JobID }");
                return BadRequest(new[] { "Error disabling users for sync job." });
            }

            return Ok();
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] StopSyncRequestData data)
        {
            var job = await _db.FindAsync<Sync.SyncJob>(data.JobID);
            if(job != null && job.Status == Sync.SyncJobStatus.Running)
            {
                job.Stop();
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        public class StopSyncRequestData
        {
            public Guid JobID { get; set; }
        }

    }
}
