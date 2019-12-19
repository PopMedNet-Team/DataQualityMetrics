using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.SyncData
{
    public partial class CreateInitialSyncSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Start = table.Column<DateTimeOffset>(nullable: false),
                    End = table.Column<DateTimeOffset>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogItems",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ItemID = table.Column<Guid>(nullable: false),
                    Action = table.Column<int>(nullable: false),
                    JobID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SyncLogItems_SyncJobs_JobID",
                        column: x => x.JobID,
                        principalTable: "SyncJobs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogItems_JobID",
                table: "SyncLogItems",
                column: "JobID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLogItems");

            migrationBuilder.DropTable(
                name: "SyncJobs");
        }
    }
}
