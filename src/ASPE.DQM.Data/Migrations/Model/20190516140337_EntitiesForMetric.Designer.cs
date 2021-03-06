﻿// <auto-generated />
using System;
using ASPE.DQM.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ASPE.DQM.Migrations.Model
{
    [DbContext(typeof(ModelDataContext))]
    [Migration("20190516140337_EntitiesForMetric")]
    partial class EntitiesForMetric
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ASPE.DQM.Model.DataQualityFrameworkCategory", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<string>("SubCategory");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("DataQualityFrameworkCategories");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Domain", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("Domains");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<Guid>("AuthorID");

                    b.Property<string>("CDM");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("Description");

                    b.Property<string>("Justification");

                    b.Property<DateTime>("ModifiedOn");

                    b.Property<string>("RelevantColumn");

                    b.Property<string>("RelevantTable");

                    b.Property<string>("ServiceDeskUrl");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.HasIndex("AuthorID");

                    b.ToTable("Metrics");
                });

            modelBuilder.Entity("ASPE.DQM.Model.MetricResultsType", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Value");

                    b.HasKey("ID");

                    b.ToTable("MetricResultTypes");
                });

            modelBuilder.Entity("ASPE.DQM.Model.MetricStatus", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<int>("Access");

                    b.Property<int>("Order");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("MetricStatuses");
                });

            modelBuilder.Entity("ASPE.DQM.Model.MetricStatusItem", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<DateTime>("CreateOn");

                    b.Property<Guid>("MetricID");

                    b.Property<Guid>("MetricStatusID");

                    b.Property<string>("Note");

                    b.Property<Guid?>("PreviousMetricStatusID");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<Guid>("UserID");

                    b.HasKey("ID");

                    b.HasIndex("MetricID");

                    b.HasIndex("MetricStatusID");

                    b.HasIndex("PreviousMetricStatusID");

                    b.HasIndex("UserID");

                    b.ToTable("MetricStatusItems");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_DataQualityFrameworkCategory", b =>
                {
                    b.Property<Guid>("MetricID");

                    b.Property<Guid>("DataQualityFrameworkCategoryID");

                    b.HasKey("MetricID", "DataQualityFrameworkCategoryID");

                    b.HasIndex("DataQualityFrameworkCategoryID");

                    b.ToTable("Metric_DataQualityFrameworkCategories");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_Domain", b =>
                {
                    b.Property<Guid>("MetricID");

                    b.Property<Guid>("DomainID");

                    b.HasKey("MetricID", "DomainID");

                    b.HasIndex("DomainID");

                    b.ToTable("Metric_Domains");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_MetricResultsType", b =>
                {
                    b.Property<Guid>("MetricID");

                    b.Property<Guid>("MetricResultTypeID");

                    b.HasKey("MetricID", "MetricResultTypeID");

                    b.HasIndex("MetricResultTypeID");

                    b.ToTable("Metric_MetricResultsTypes");
                });

            modelBuilder.Entity("ASPE.DQM.Model.User", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<string>("Email");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<string>("Organization");

                    b.Property<string>("PhoneNumber");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("UserName");

                    b.HasKey("ID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Visualization", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<string>("AppID");

                    b.Property<string>("Description");

                    b.Property<bool>("Published");

                    b.Property<bool>("RequiresAuth");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("Visualizations");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric", b =>
                {
                    b.HasOne("ASPE.DQM.Model.User", "Author")
                        .WithMany("AuthoredMetrics")
                        .HasForeignKey("AuthorID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ASPE.DQM.Model.MetricStatusItem", b =>
                {
                    b.HasOne("ASPE.DQM.Model.Metric", "Metric")
                        .WithMany("Statuses")
                        .HasForeignKey("MetricID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.MetricStatus", "MetricStatus")
                        .WithMany()
                        .HasForeignKey("MetricStatusID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.MetricStatusItem", "PreviousMetricStatus")
                        .WithMany()
                        .HasForeignKey("PreviousMetricStatusID");

                    b.HasOne("ASPE.DQM.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_DataQualityFrameworkCategory", b =>
                {
                    b.HasOne("ASPE.DQM.Model.DataQualityFrameworkCategory", "DataQualityFrameworkCategory")
                        .WithMany("Metrics")
                        .HasForeignKey("DataQualityFrameworkCategoryID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.Metric", "Metric")
                        .WithMany("FrameworkCategories")
                        .HasForeignKey("MetricID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_Domain", b =>
                {
                    b.HasOne("ASPE.DQM.Model.Domain", "Domain")
                        .WithMany("Metrics")
                        .HasForeignKey("DomainID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.Metric", "Metric")
                        .WithMany("Domains")
                        .HasForeignKey("MetricID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric_MetricResultsType", b =>
                {
                    b.HasOne("ASPE.DQM.Model.Metric", "Metric")
                        .WithMany("ResultsTypes")
                        .HasForeignKey("MetricID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.MetricResultsType", "MetricResultsType")
                        .WithMany("Metrics")
                        .HasForeignKey("MetricResultTypeID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
