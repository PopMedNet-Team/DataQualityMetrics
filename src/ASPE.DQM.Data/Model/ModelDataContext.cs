using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Model
{
    public class ModelDataContext : DbContext
    {
        public DbSet<DataQualityFrameworkCategory> DataQualityFrameworkCategories { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<MetricStatusItem> MetricStatusItems { get; set; }
        public DbSet<MetricStatus> MetricStatuses { get; set; }
        public DbSet<MetricResultsType> MetricResultTypes { get; set; }
        public DbSet<Metric> Metrics { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Visualization> Visualizations { get; set; }
        public DbSet<MeasurementMeta> MeasurementMeta { get; set; }
        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<UserVisualizationFavorite> UserFavoriteVisualizations { get; set; }
        public DbSet<UserMetricFavorite> UserFavoriteMetrics { get; set; }



        public ModelDataContext(DbContextOptions<ModelDataContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Visualization>(t => {
                t.ToTable("Visualizations");
            });

            modelBuilder.Entity<DataQualityFrameworkCategory>(t => {
                t.ToTable("DataQualityFrameworkCategories");
            });

            modelBuilder.Entity<Domain>(t => {
                t.ToTable("Domains");
            });

            modelBuilder.Entity<MetricResultsType>(t => {
                t.ToTable("MetricResultTypes");

                t.HasMany(r => r.Metrics)
                .WithOne(m => m.ResultsType)
                .HasForeignKey(m => m.ResultsTypeID)
                .IsRequired(true);
            });

            modelBuilder.Entity<MetricStatus>(t => {
                t.ToTable("MetricStatuses");
            });

            modelBuilder.Entity<MetricStatusItem>(t => {
                t.ToTable("MetricStatusItems");
            });

            modelBuilder.Entity<Metric>(t => {
                t.ToTable("Metrics");

                t.HasMany(m => m.Statuses)
                .WithOne(s => s.Metric)
                .HasForeignKey(s => s.MetricID)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

                t.HasMany(m => m.Measurements)
                .WithOne(m => m.Metric)
                .HasForeignKey(m => m.MetricID)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<Metric_DataQualityFrameworkCategory>(t => {
                t.ToTable("Metric_DataQualityFrameworkCategories");

                t.HasKey(k => new { k.MetricID, k.DataQualityFrameworkCategoryID });

                t.HasOne(k => k.Metric)
                .WithMany(m => m.FrameworkCategories)
                .HasForeignKey(k => k.MetricID);

                t.HasOne(k => k.DataQualityFrameworkCategory)
                .WithMany(r => r.Metrics)
                .HasForeignKey(k => k.DataQualityFrameworkCategoryID);
            });

            modelBuilder.Entity<Metric_Domain>(t => {
                t.ToTable("Metric_Domains");

                t.HasKey(k => new { k.MetricID, k.DomainID });

                t.HasOne(k => k.Metric)
                .WithMany(m => m.Domains)
                .HasForeignKey(k => k.MetricID);

                t.HasOne(k => k.Domain)
                .WithMany(r => r.Metrics)
                .HasForeignKey(k => k.DomainID);
            });

            modelBuilder.Entity<UserVisualizationFavorite>(t => {
                t.ToTable("User_VisualizationFavorites");

                t.HasKey(k => new { k.UserID, k.VisualizationID });

                t.HasOne(k => k.Visualization);
            });

            modelBuilder.Entity<UserMetricFavorite>(t => {
                t.ToTable("User_MetricFavorites");

                t.HasKey(k => new { k.UserID, k.MetricID });

                t.HasOne(k => k.Metric);
            });

            modelBuilder.Entity<User>(t => {
                t.ToTable("Users");
                t.HasMany(u => u.AuthoredMetrics)
                .WithOne(m => m.Author)
                .HasForeignKey(m => m.AuthorID)
                .IsRequired(true);

                t.HasMany(u => u.Documents).WithOne(d => d.UploadedBy).HasForeignKey(d => d.UploadedByID).IsRequired(false);

                t.HasMany(u => u.Measurements).WithOne(m => m.SubmittedBy).HasForeignKey(m => m.SubmittedByID).IsRequired(true);

                t.HasMany(u => u.FavoriteVisualizations).WithOne(f => f.User).HasForeignKey(v => v.UserID).IsRequired(true);
                t.HasMany(u => u.FavoriteMetrics).WithOne(f => f.User).HasForeignKey(m => m.UserID).IsRequired(true);
            });

            modelBuilder.Entity<Document>(m =>
            {
                m.ToTable("Documents");
                m.Property(d => d.Name).IsRequired();
                m.Property(d => d.FileName).IsRequired();
                m.Property(d => d.Viewable).IsRequired().HasColumnName("isViewable");

                m.HasIndex(x => x.Name);
                m.HasIndex(x => x.FileName);

                m.HasMany(d => d.Documents).WithOne(d => d.ParentDocument).HasForeignKey(d => d.ParentDocumentID).IsRequired(false);
            });

            modelBuilder.Entity<Measurement>(t => {
                t.ToTable("Measurements");

                t.Property(m => m.Definition).IsRequired().HasMaxLength(500);
                t.Property(m => m.RawValue).IsRequired().HasMaxLength(500);
                t.Property(m => m.Measure).IsRequired();
                t.Property(m => m.Total).IsRequired(false);
            });

            modelBuilder.Entity<MeasurementMeta>(t => {
                t.ToTable("MeasurementMeta");

                t.Property(m => m.Organization).IsRequired().HasMaxLength(250);
                t.Property(m => m.DataSource).IsRequired().HasMaxLength(250);
                t.Property(m => m.CommonDataModelVersion).HasMaxLength(255);
                t.Property(m => m.ResultsDelimiter).HasMaxLength(5);

                t.HasMany(m => m.Measurements)
                .WithOne(mm => mm.Metadata)
                .HasForeignKey(mm => mm.MetadataID)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
