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

namespace ASPE.DQM.Test
{
    [TestClass]
    public class UserIdentityTests
    {
        [TestMethod]
        public void SetupUserManager()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(System.AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config)
                .AddDbContext<Identity.IdentityContext>(o => o.UseSqlServer(config.GetConnectionString("IdentityContextConnection")))
                .AddIdentityCore<Identity.IdentityUser>(o => { })
                .AddEntityFrameworkStores<Identity.IdentityContext>();

            services.AddLogging();

            var provider = services.BuildServiceProvider();
            var builder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(provider);

            using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using(var db = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var userStore = builder.ApplicationServices.GetRequiredService<IUserStore<Identity.IdentityUser>>();
                var userManager = builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();

                Assert.IsNotNull(userStore);
                Assert.IsNotNull(userManager);
            }
        }

        [TestMethod]
        public async Task CreateUser()
        {
            var appFactory = new ApplicationFactory();
            Assert.IsNotNull(appFactory.Provider);
            Assert.IsNotNull(appFactory.Builder);

            string username = "billyBob";
            string password = "Password1!";
            string email = "billy.bob@gdit.com";

            using (var scoped = appFactory.CreateScope())
            using(var db = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var userStore = appFactory.Builder.ApplicationServices.GetRequiredService<IUserStore<Identity.IdentityUser>>();
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();

                Assert.IsNotNull(userStore);
                Assert.IsNotNull(userManager);

                var user = new Identity.IdentityUser { UserName = username, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);

                Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors.Select(err => err.Description).ToArray()));
            }

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var userStore = appFactory.Builder.ApplicationServices.GetRequiredService<IUserStore<Identity.IdentityUser>>();
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();

                Assert.IsNotNull(userStore);
                Assert.IsNotNull(userManager);

                var user = await userManager.FindByNameAsync(username);
                Assert.IsNotNull(user, "User not found by username!");

                
            }

        }

        [TestMethod]
        public async Task CreateUser2()
        {
            var appFactory = new ApplicationFactory();
            Assert.IsNotNull(appFactory.Provider);
            Assert.IsNotNull(appFactory.Builder);

            Guid id = new Guid("2CBF97E0-FF50-496A-8F77-A57DA62DAC05");
            string username = "SystemAdministrator";
            string password = "Password1!";
            string email = "SystemAdministrator@gdit.com";

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var userStore = appFactory.Builder.ApplicationServices.GetRequiredService<IUserStore<Identity.IdentityUser>>();
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();

                Assert.IsNotNull(userStore);
                Assert.IsNotNull(userManager);

                var user = new Identity.IdentityUser { Id = id, UserName = username, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);

                Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors.Select(err => err.Description).ToArray()));

                var claimResult = await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("System Administrator", "true"));

                Assert.IsTrue(claimResult.Succeeded, string.Join(Environment.NewLine, claimResult.Errors.Select(err => err.Description).ToArray()));
            }

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var userStore = appFactory.Builder.ApplicationServices.GetRequiredService<IUserStore<Identity.IdentityUser>>();
                var userManager = appFactory.Builder.ApplicationServices.GetRequiredService<UserManager<Identity.IdentityUser>>();

                Assert.IsNotNull(userStore);
                Assert.IsNotNull(userManager);

                var user = await userManager.FindByNameAsync(username);
                Assert.IsNotNull(user, "User not found by username!");


            }

        }

        [TestMethod]
        public async Task PasswordHashing()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
                using(var dg = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                var passwordHasher = appFactory.Builder.ApplicationServices.GetRequiredService<IPasswordHasher<Identity.IdentityUser>>();

                var user = await dg.Users.FirstOrDefaultAsync(u => u.UserName == "billyBob");

                Console.WriteLine("Database hash:\t" + user.PasswordHash);

                var hashedPassword = passwordHasher.HashPassword(user, "Password1!");

                Console.WriteLine("Provider hash:\t" + hashedPassword);

                //the newly created hash will not be the same as the database version, looks like time is included in the salt for the password hash                
                Assert.AreEqual(user.PasswordHash, hashedPassword);

                var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "Password1!");
                Console.WriteLine("Verification result: " + ((PasswordVerificationResult)verificationResult).ToString());
                Assert.AreEqual(PasswordVerificationResult.Success, verificationResult);
            
            }
        }
    }
}
