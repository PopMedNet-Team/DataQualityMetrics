using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ASPE.DQM.Identity
{
    public class IdentityContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<IdentityUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
    {
        public IdentityContext(Microsoft.EntityFrameworkCore.DbContextOptions<IdentityContext> options): base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
