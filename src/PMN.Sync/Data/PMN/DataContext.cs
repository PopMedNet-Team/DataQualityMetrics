using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMN.Sync.Data.PMN
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<SecurityGroupUser> SecurityGroupUsers { get; set; }

        public DbSet<UserChangeLog> UserChangeLogs { get; set; }

        public DbSet<ProfileUpdatedLog> UserProfileUpdatedLogs { get; set; }

        public DbSet<UserRegistrationChangedLog> UserRegistrationChangedLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(u => {
                u.ToTable("Users");
                u.Property(p => p.Deleted).HasColumnName("isDeleted");
                u.Property(p => p.Active).HasColumnName("isActive");
                u.Property(p => p.PasswordHash).HasColumnName("Password");

                u.HasMany(p => p.SecurityGroups)
                    .WithOne(sgu => sgu.User)
                    .HasForeignKey(sgu => sgu.UserID)
                    .IsRequired(true);

                u.HasMany(p => p.UserChangeLogs)
                    .WithOne(l => l.UserChanged)
                    .HasForeignKey(l => l.UserChangedID)
                    .IsRequired(true);

                u.HasMany(p => p.ProfileUpdatedLogs)
                    .WithOne(l => l.UserChanged)
                    .HasForeignKey(l => l.UserChangedID)
                    .IsRequired(true);

                u.HasMany(p => p.RegistrationChangedLogs)
                    .WithOne(l => l.RegisteredUser)
                    .HasForeignKey(l => l.RegisteredUserID)
                    .IsRequired(true);

            });

            modelBuilder.Entity<SecurityGroupUser>()
                .ToTable("SecurityGroupUsers")
                .HasKey(sgu => new { sgu.SecurityGroupID, sgu.UserID });

            modelBuilder.Entity<UserChangeLog>(l => {
                l.ToTable("LogsUserChange");                
                l.HasKey(k => new { k.UserID, k.TimeStamp, k.UserChangedID });
                l.HasIndex(i => i.UserChangedID);
            });

            modelBuilder.Entity<ProfileUpdatedLog>(l =>
            {
                l.ToTable("LogsProfileUpdated");
                l.HasKey(k => new { k.UserID, k.TimeStamp, k.UserChangedID });
                l.HasIndex(i => i.UserChangedID);
            });

            modelBuilder.Entity<UserRegistrationChangedLog>(l =>
            {
                l.ToTable("LogsUserRegistrationChanged");
                l.HasKey(k => new { k.UserID, k.TimeStamp, k.RegisteredUserID });
                l.HasIndex(i => i.RegisteredUserID);
            });

            modelBuilder.Entity<Organization>(o => {
                o.ToTable("Organizations");
                o.HasKey(k => k.ID);
                o.Property(org => org.Deleted).HasColumnName("IsDeleted");
                o.HasMany(org => org.Users)
                .WithOne(u => u.Organization)
                .HasForeignKey(u => u.OrganizationID)
                .IsRequired(false);
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
