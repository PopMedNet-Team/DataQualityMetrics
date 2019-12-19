﻿// <auto-generated />
using System;
using ASPE.DQM.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ASPE.DQM.Migrations.SyncData
{
    [DbContext(typeof(SyncDataContext))]
    [Migration("20190206215656_CreateInitialSyncSchema")]
    partial class CreateInitialSyncSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ASPE.DQM.Sync.SyncJob", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset?>("End");

                    b.Property<string>("Message");

                    b.Property<DateTimeOffset>("Start");

                    b.Property<int>("Status");

                    b.HasKey("ID");

                    b.ToTable("SyncJobs");
                });

            modelBuilder.Entity("ASPE.DQM.Sync.SyncLogItem", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Action");

                    b.Property<string>("Description");

                    b.Property<Guid>("ItemID");

                    b.Property<Guid>("JobID");

                    b.Property<DateTimeOffset>("Timestamp");

                    b.HasKey("ID");

                    b.HasIndex("JobID");

                    b.ToTable("SyncLogItems");
                });

            modelBuilder.Entity("ASPE.DQM.Sync.SyncLogItem", b =>
                {
                    b.HasOne("ASPE.DQM.Sync.SyncJob", "Job")
                        .WithMany("Items")
                        .HasForeignKey("JobID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
