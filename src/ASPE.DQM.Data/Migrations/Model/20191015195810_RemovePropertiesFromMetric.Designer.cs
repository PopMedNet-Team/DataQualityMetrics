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
    [Migration("20191015195810_RemovePropertiesFromMetric")]
    partial class RemovePropertiesFromMetric
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

            modelBuilder.Entity("ASPE.DQM.Model.Document", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<int>("BuildVersion");

                    b.Property<int>("ChunkCount");

                    b.Property<DateTime?>("ContentCreatedOn");

                    b.Property<DateTime?>("ContentModifiedOn");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("Description");

                    b.Property<string>("FileName")
                        .IsRequired();

                    b.Property<Guid>("ItemID");

                    b.Property<string>("Kind");

                    b.Property<long>("Length");

                    b.Property<int>("MajorVersion");

                    b.Property<string>("MimeType");

                    b.Property<int>("MinorVersion");

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<Guid?>("ParentDocumentID");

                    b.Property<string>("RevisionDescription");

                    b.Property<Guid?>("RevisionSetID");

                    b.Property<int>("RevisionVersion");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<Guid?>("UploadedByID");

                    b.Property<bool>("Viewable")
                        .HasColumnName("isViewable");

                    b.HasKey("ID");

                    b.HasIndex("FileName");

                    b.HasIndex("Name");

                    b.HasIndex("ParentDocumentID");

                    b.HasIndex("UploadedByID");

                    b.ToTable("Documents");
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

            modelBuilder.Entity("ASPE.DQM.Model.Measurement", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<string>("Definition")
                        .IsRequired()
                        .HasMaxLength(500);

                    b.Property<float>("Measure");

                    b.Property<Guid>("MetadataID");

                    b.Property<string>("RawValue")
                        .IsRequired()
                        .HasMaxLength(500);

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<float?>("Total");

                    b.HasKey("ID");

                    b.HasIndex("MetadataID");

                    b.ToTable("Measurements");
                });

            modelBuilder.Entity("ASPE.DQM.Model.MeasurementMeta", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<string>("CommonDataModel");

                    b.Property<string>("CommonDataModelVersion")
                        .HasMaxLength(255);

                    b.Property<string>("DataSource")
                        .IsRequired()
                        .HasMaxLength(250);

                    b.Property<Guid?>("DataSourceID");

                    b.Property<string>("DatabaseSystem");

                    b.Property<DateTime>("DateRangeEnd");

                    b.Property<DateTime>("DateRangeStart");

                    b.Property<string>("EvaluatedVariableDataType");

                    b.Property<Guid>("MetricID");

                    b.Property<string>("Network");

                    b.Property<string>("Organization")
                        .IsRequired()
                        .HasMaxLength(250);

                    b.Property<Guid?>("OrganizationID");

                    b.Property<string>("ResultsDelimiter")
                        .HasMaxLength(5);

                    b.Property<Guid>("ResultsTypeID");

                    b.Property<DateTime>("RunDate");

                    b.Property<Guid>("SubmittedByID");

                    b.Property<DateTime>("SubmittedOn");

                    b.Property<Guid?>("SuspendedByID");

                    b.Property<DateTime?>("SuspendedOn");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.HasKey("ID");

                    b.HasIndex("MetricID");

                    b.HasIndex("SubmittedByID");

                    b.HasIndex("SuspendedByID");

                    b.ToTable("MeasurementMeta");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric", b =>
                {
                    b.Property<Guid>("ID");

                    b.Property<Guid>("AuthorID");

                    b.Property<DateTime>("CreatedOn");

                    b.Property<string>("Description");

                    b.Property<string>("ExpectedResults");

                    b.Property<string>("Justification");

                    b.Property<DateTime>("ModifiedOn");

                    b.Property<Guid>("ResultsTypeID");

                    b.Property<string>("ServiceDeskUrl");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.HasIndex("AuthorID");

                    b.HasIndex("ResultsTypeID");

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

                    b.Property<bool>("AllowEdit");

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

                    b.Property<string>("SheetID");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("Title");

                    b.HasKey("ID");

                    b.ToTable("Visualizations");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Document", b =>
                {
                    b.HasOne("ASPE.DQM.Model.Document", "ParentDocument")
                        .WithMany("Documents")
                        .HasForeignKey("ParentDocumentID");

                    b.HasOne("ASPE.DQM.Model.User", "UploadedBy")
                        .WithMany("Documents")
                        .HasForeignKey("UploadedByID");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Measurement", b =>
                {
                    b.HasOne("ASPE.DQM.Model.MeasurementMeta", "Metadata")
                        .WithMany("Measurements")
                        .HasForeignKey("MetadataID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ASPE.DQM.Model.MeasurementMeta", b =>
                {
                    b.HasOne("ASPE.DQM.Model.Metric", "Metric")
                        .WithMany("Measurements")
                        .HasForeignKey("MetricID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ASPE.DQM.Model.User", "SubmittedBy")
                        .WithMany("Measurements")
                        .HasForeignKey("SubmittedByID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.User", "SuspendedBy")
                        .WithMany()
                        .HasForeignKey("SuspendedByID");
                });

            modelBuilder.Entity("ASPE.DQM.Model.Metric", b =>
                {
                    b.HasOne("ASPE.DQM.Model.User", "Author")
                        .WithMany("AuthoredMetrics")
                        .HasForeignKey("AuthorID")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ASPE.DQM.Model.MetricResultsType", "ResultsType")
                        .WithMany("Metrics")
                        .HasForeignKey("ResultsTypeID")
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
#pragma warning restore 612, 618
        }
    }
}
