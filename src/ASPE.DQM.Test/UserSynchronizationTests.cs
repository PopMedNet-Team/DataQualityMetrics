using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System;
using ASPE.DQM.Sync;

namespace ASPE.DQM.Test
{
    [TestClass]
    public class UserSynchronizationTests
    {

        [TestMethod]
        public async Task CreateUsers()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var syncDB = scoped.ServiceProvider.GetRequiredService<Sync.SyncDataContext>())
            {
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();
                var passwordHasher = appFactory.Builder.ApplicationServices.GetRequiredService<IPasswordHasher<Identity.IdentityUser>>();

                //create the sync job
                var job = new Sync.SyncJob();
                syncDB.SyncJobs.Add(job);
                await syncDB.SaveChangesAsync();

                List<Sync.IUserSyncItem> usersToSync = new List<Sync.IUserSyncItem>();
                for (int i = 0; i < 10; i++) {
                    Guid userID = Guid.NewGuid();
                    usersToSync.Add(new TestSyncUserItem { ID = userID, UserName = "AutoTest-" + userID.ToString("D"), Email = userID.ToString("D") + "@autotest.test", PasswordHash = passwordHasher.HashPassword(null, "Password1!") });
                }

                var errors = await job.SyncUsersAsync(userManager, usersToSync);

                //save the log items, and end the job
                if (errors != null && errors.Any())
                {
                    job.StopWithErrors("There were errors processing the user sync.");
                }
                else
                {
                    job.Stop();
                }

                await syncDB.SaveChangesAsync();

                if(errors != null && errors.Any())
                {
                    foreach(var error in errors)
                    {
                        Console.WriteLine(error);
                    }

                    Assert.Fail("There were errors syncing the users.");
                }


            }
        }

        [TestMethod]
        public async Task CreateAndUpdateUser()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var syncDB = scoped.ServiceProvider.GetRequiredService<Sync.SyncDataContext>())
            {
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();
                var passwordHasher = appFactory.Builder.ApplicationServices.GetRequiredService<IPasswordHasher<Identity.IdentityUser>>();

                //create the sync job
                var job = new Sync.SyncJob();
                syncDB.SyncJobs.Add(job);
                await syncDB.SaveChangesAsync();

                List<Sync.IUserSyncItem> usersToSync = new List<Sync.IUserSyncItem>();
                Guid userID = Guid.NewGuid();
                usersToSync.Add(new TestSyncUserItem { ID = userID, UserName = "AutoTest-" + userID.ToString("D"), Email = userID.ToString("D") + "@autotest.test", PasswordHash = passwordHasher.HashPassword(null, "Password1!") });
                usersToSync.Add(new TestSyncUserItem { ID = userID, UserName = "AutoTest-Changed", Email = userID.ToString("D") + ".changed@autotest.test", PasswordHash = passwordHasher.HashPassword(null, "Password2@") });

                var errors = await job.SyncUsersAsync(userManager, usersToSync);

                //save the log items, and end the job
                if (errors != null && errors.Any())
                {
                    job.StopWithErrors("There were errors processing the user sync.");
                }
                else
                {
                    job.Stop();
                }

                await syncDB.SaveChangesAsync();

                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error);
                    }

                    Assert.Fail("There were errors syncing the users.");
                }


            }
        }

        internal class TestSyncUserItem : Sync.IUserSyncItem
        {
            public Guid ID { get; set; }

            public string UserName { get; set; }

            public string Email { get; set; }

            public string PasswordHash { get; set; }
            public IEnumerable<KeyValuePair<string, string>> Claims { get; set; }

            public string GetID()
            {
                return ID.ToString("D").ToLower();
            }
        }

    }
}
