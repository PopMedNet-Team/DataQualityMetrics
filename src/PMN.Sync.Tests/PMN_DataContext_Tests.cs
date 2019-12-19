using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PMN.Sync.Tests
{
    [TestClass]
    public class PMN_DataContext_Tests
    {
        [TestMethod]
        public void CountActiveUsers()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var pmnDB = scoped.ServiceProvider.GetRequiredService<Data.PMN.DataContext>())
            {
                var logger = scoped.ServiceProvider.GetRequiredService<ILogger<PMN_DataContext_Tests>>();

                var q = from u in pmnDB.Users
                        where u.Active && u.Deleted == false
                        select u;
                
                var count = q.Count();
                logger.LogDebug("Total active users: " + count);

            }

        }
    }
}
