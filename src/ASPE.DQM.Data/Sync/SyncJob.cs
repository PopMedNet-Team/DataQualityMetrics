using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Sync
{
    /// <summary>
    /// A synchronization job.
    /// </summary>
    public class SyncJob : ISyncJob
    {

        public SyncJob()
        {
            ID = Model.EntityWithID.NewGuid();
            Status = SyncJobStatus.Running;
            Start = DateTimeOffset.Now;
        }

        /// <summary>
        /// The unique ID of the synchronization job.
        /// </summary>
        [Key]
        public Guid ID { get; set; }
        /// <summary>
        /// The DateTime the synchronization job started.
        /// </summary>
        public DateTimeOffset Start { get; set; }
        /// <summary>
        /// The DateTime the synchronization job stopped, either by completing successfully or due to an error.
        /// </summary>
        public DateTimeOffset? End { get; set; }
        /// <summary>
        /// The status of the synchronization job.
        /// </summary>
        public SyncJobStatus Status { get; set; }
        /// <summary>
        /// A message associated to the job.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The synchronization log items for each entity synched.
        /// </summary>
        public ICollection<SyncLogItem> Items { get; } = new HashSet<SyncLogItem>();


        public void Stop()
        {
            End = DateTimeOffset.Now;
            Status = SyncJobStatus.Success;
        }

        public void StopWithErrors(string message)
        {
            End = DateTimeOffset.Now;
            Status = SyncJobStatus.StoppedWithError;
            Message = message;
        }

        /// <summary>
        /// Updates users in DQM.
        /// </summary>
        /// <param name="userManager"></param>
        /// <returns>A collection of error messages, if empty the sync was successful.</returns>
        public async Task<IEnumerable<string>> SyncUsersAsync(UserManager<Identity.IdentityUser> userManager, IEnumerable<IUserSyncItem> usersToSync)
        {
            List<string> errors = new List<string>();
            IdentityResult actionResult;

            try
            {
                foreach (var userData in usersToSync)
                {
                    Identity.IdentityUser user = await userManager.FindByIdAsync(userData.GetID());

                    if (user == null)
                    {
                        //add the user to DQM
                        user = new Identity.IdentityUser
                        {
                            Id = userData.ID,
                            Email = userData.Email,
                            UserName = userData.UserName,
                            EmailConfirmed = true
                        };

                        //create without the password hash set
                        actionResult = await userManager.CreateAsync(user);
                        if (actionResult.Succeeded != true)
                        {
                            errors.Add($"Error creating new user (ID: { userData.GetID() } ): " + string.Join(Environment.NewLine, actionResult.Errors.Select(err => err.Description)));
                            break;
                        }

                        this.Items.Add(new SyncLogItem
                        {
                            Action = SyncAction.Added,
                            ItemID = userData.ID,
                            JobID = this.ID,
                            Description = $"Add user with ID: \"{ userData.GetID() }\", Username: \"{ userData.UserName }\"."
                        });

                    }
                    else
                    {
                        if (!string.Equals(user.UserName + user.Email + user.PasswordHash, userData.UserName + userData.Email + userData.PasswordHash, StringComparison.Ordinal))
                        {
                            //something has been updated for an existing user, log
                            this.Items.Add(new SyncLogItem
                            {
                                Action = SyncAction.Modified,
                                ItemID = userData.ID,
                                JobID = this.ID,
                                Description = $"Updating user with ID: \"{ userData.GetID() }\", Username: \"{ userData.UserName }\"."
                            });
                        }

                        if (!string.Equals(user.UserName, userData.UserName, StringComparison.Ordinal))
                        {
                            //update the user information
                            user.UserName = userData.UserName;
                        }

                        if (!string.Equals(user.Email, userData.Email, StringComparison.Ordinal))
                        {
                            //update the user information
                            user.Email = userData.Email;
                        }

                        if (user.LockoutEnd.HasValue)
                        {
                            //using the lockout to handle user's soft deleted in PMN
                            user.LockoutEnd = null;

                            this.Items.Add(new SyncLogItem
                            {
                                Action = SyncAction.Modified,
                                ItemID = userData.ID,
                                JobID = this.ID,
                                Description = $"Re-enabling user with ID: \"{ userData.GetID() }\", Username: \"{ userData.UserName }\"."
                            });
                        }
                    }

                    if (!string.Equals(user.PasswordHash, userData.PasswordHash, StringComparison.Ordinal))
                    {
                        //update the password hash
                        user.PasswordHash = userData.PasswordHash;
                        actionResult = await userManager.UpdateAsync(user);
                        if (actionResult.Succeeded != true)
                        {
                            errors.Add($"Error updating password for user (ID: { userData.GetID() }, Username: { userData.UserName } ): " + string.Join(Environment.NewLine, actionResult.Errors.Select(err => err.Description)));
                            break;
                        }
                        else
                        {
                            this.Items.Add(new SyncLogItem
                            {
                                Action = SyncAction.Modified,
                                ItemID = userData.ID,
                                JobID = this.ID,
                                Description = $"Updated password for user with ID: \"{ userData.GetID() }\", Username: \"{ userData.UserName }\"."
                            });
                        }
                    }                    

                    actionResult = await userManager.UpdateSecurityStampAsync(user);
                    if (actionResult.Succeeded != true)
                    {
                        errors.Add($"Error updating security stamp for user (ID: { userData.GetID() }, Username: { userData.UserName } ): " + string.Join(Environment.NewLine, actionResult.Errors.Select(err => err.Description)));
                        break;
                    }

                    //update the claims for the user
                    var claims = await userManager.GetClaimsAsync(user);
                    var syncClaims = (userData.Claims ?? Enumerable.Empty<KeyValuePair<string, string>>()).Where(c => Identity.Claims.Keys.Contains(c.Key, StringComparer.OrdinalIgnoreCase)).ToArray();

                    var claimsToAdd = new List<System.Security.Claims.Claim>();

                    var claimsToRemove = (from c in claims
                                            where !syncClaims.Any(sc => string.Equals(sc.Key, c.Type, StringComparison.OrdinalIgnoreCase))
                                            select c).ToList();                        

                    Action<KeyValuePair<string,string>> updateClaim = (KeyValuePair<string,string> pair) => {

                        //get the key from the definitions so that the casing matches
                        var claimType = Identity.Claims.Keys.FirstOrDefault(k => string.Equals(k, pair.Key, StringComparison.OrdinalIgnoreCase));

                        var claim = claims.FirstOrDefault(cl => cl.Type == claimType);
                        if (claim == null)
                        {
                            claimsToAdd.Add(new System.Security.Claims.Claim(claimType, pair.Value));
                        }
                        else if (claim.Value != pair.Value)
                        {
                            claimsToRemove.AddRange(claims.Where(cl => cl.Type == claimType));
                            claimsToAdd.Add(new System.Security.Claims.Claim(claimType, pair.Value));
                        }
                    };

                    foreach(var syncClaim in syncClaims)
                    {
                        updateClaim(syncClaim);
                    }

                    if (claimsToRemove.Count > 0)
                    {
                        actionResult = await userManager.RemoveClaimsAsync(user, claimsToRemove);

                        if (actionResult.Succeeded != true)
                        {
                            errors.AddRange(actionResult.Errors.Select(err => $"Error removing claims ({ string.Join(", ", claimsToRemove.Select(c => c.Type)) }) from user (ID: { userData.GetID() }, Username: { userData.UserName } ): {err.Description}"));
                        }
                        else
                        {
                            this.Items.Add(new SyncLogItem
                            {
                                Action = SyncAction.Modified,
                                ItemID = userData.ID,
                                JobID = this.ID,
                                Description = $"User claims REMOVED to user (ID: { userData.GetID() }, Username: { userData.UserName }): { string.Join(", ", claimsToRemove.Select(c => c.Type)) }"
                            });
                        }
                    }

                    if (claimsToAdd.Count > 0)
                    {
                        actionResult = await userManager.AddClaimsAsync(user, claimsToAdd);
                        if(actionResult.Succeeded != true)
                        {
                            errors.AddRange(actionResult.Errors.Select(err => $"Error adding claims ({ string.Join(", ", claimsToAdd.Select(c => c.Type)) }) to user (ID: { userData.GetID() }, Username: { userData.UserName } ): {err.Description}"));
                        }
                        else
                        {
                            this.Items.Add(new SyncLogItem {
                                Action = SyncAction.Modified,
                                ItemID = userData.ID,
                                JobID = this.ID,
                                Description = $"User claims ADDED to user (ID: { userData.GetID() }, Username: { userData.UserName }): { string.Join(", ", claimsToAdd.Select(c => c.Type)) }"
                            });
                        }
                    }

                }
            }
            catch (Exception ex) {
                errors.Add(ex.ToString());
            }

            if (errors.Any())
            {
                this.StopWithErrors(string.Join(Environment.NewLine, errors.ToArray()));
            }

            return errors;
        }

        public async Task<IEnumerable<string>> DisableUsersAsync(UserManager<Identity.IdentityUser> userManager, IEnumerable<IUserSyncItem> usersToDisable)
        {
            List<string> errors = new List<string>();
            IdentityResult actionResult;

            try
            {
                foreach(var userData in usersToDisable)
                {
                    string userID = userData.ID.ToString("D").ToLower();
                    Identity.IdentityUser user = await userManager.FindByIdAsync(userID);

                    if(user != null)
                    {
                        user.LockoutEnd = new DateTime(2099, 12, 31);

                        actionResult = await userManager.UpdateAsync(user);
                        if (actionResult.Succeeded != true)
                        {
                            errors.Add($"Error disabling user (ID: { userID }, Username: { userData.UserName } ): " + string.Join(Environment.NewLine, actionResult.Errors.Select(err => err.Description)));
                            break;
                        }

                        this.Items.Add(new SyncLogItem
                        {
                            Action = SyncAction.Modified,
                            ItemID = userData.ID,
                            JobID = this.ID,
                            Description = $"Disabled user with ID: \"{ userID }\", Username: \"{ userData.UserName }\"."
                        });

                        actionResult = await userManager.UpdateSecurityStampAsync(user);
                        if (actionResult.Succeeded != true)
                        {
                            errors.Add($"Error updating security stamp for user (ID: { userID }, Username: { userData.UserName } ): " + string.Join(Environment.NewLine, actionResult.Errors.Select(err => err.Description)));
                            break;
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }

            if (errors.Any())
            {
                this.StopWithErrors(string.Join(Environment.NewLine, errors.ToArray()));
            }

            return errors;
        }


    }

    /// <summary>
    /// Synchronization job statuses.
    /// </summary>
    public enum SyncJobStatus
    {
        /// <summary>
        /// The synchronization job is running.
        /// </summary>
        Running = 0,
        /// <summary>
        /// The synchronization job is stopped with an error.
        /// </summary>
        StoppedWithError = 1,
        /// <summary>
        /// The synchronization job is finished successfully.
        /// </summary>
        Success = 2
    }    
}
