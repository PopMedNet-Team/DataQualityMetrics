using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ASPE.DQM.Sync
{
    public class SyncDataContext : DbContext
    {
        public DbSet<SyncJob> SyncJobs { get; set; }
        public DbSet<SyncLogItem> SyncLogItems { get; set; }

        public SyncDataContext(DbContextOptions<SyncDataContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SyncJob>()
                .HasMany<SyncLogItem>(j => j.Items)
                .WithOne(i => i.Job)
                .HasForeignKey(i => i.JobID)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
