using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class EntitiesForMeasures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeasurementMeta",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    MetricID = table.Column<Guid>(nullable: false),
                    OrganizationID = table.Column<Guid>(nullable: true),
                    Organization = table.Column<string>(maxLength: 250, nullable: false),
                    DataSourceID = table.Column<Guid>(nullable: true),
                    DataSource = table.Column<string>(maxLength: 250, nullable: false),
                    RunDate = table.Column<DateTime>(nullable: false),
                    Network = table.Column<string>(nullable: true),
                    CommonDataModel = table.Column<string>(nullable: true),
                    DatabaseSystem = table.Column<string>(nullable: true),
                    DateRangeStart = table.Column<DateTime>(nullable: false),
                    DateRangeEnd = table.Column<DateTime>(nullable: false),
                    ResultsTypeID = table.Column<Guid>(nullable: false),
                    SuspendedByID = table.Column<Guid>(nullable: true),
                    SuspendedOn = table.Column<DateTime>(nullable: true),
                    SubmittedByID = table.Column<Guid>(nullable: false),
                    SubmittedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementMeta", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MeasurementMeta_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeasurementMeta_Users_SubmittedByID",
                        column: x => x.SubmittedByID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementMeta_Users_SuspendedByID",
                        column: x => x.SuspendedByID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    MetadataID = table.Column<Guid>(nullable: false),
                    RawValue = table.Column<string>(maxLength: 500, nullable: false),
                    Definition = table.Column<string>(maxLength: 500, nullable: false),
                    Measure = table.Column<float>(nullable: false),
                    Total = table.Column<float>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Measurements_MeasurementMeta_MetadataID",
                        column: x => x.MetadataID,
                        principalTable: "MeasurementMeta",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementMeta_MetricID",
                table: "MeasurementMeta",
                column: "MetricID");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementMeta_SubmittedByID",
                table: "MeasurementMeta",
                column: "SubmittedByID");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementMeta_SuspendedByID",
                table: "MeasurementMeta",
                column: "SuspendedByID");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_MetadataID",
                table: "Measurements",
                column: "MetadataID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "MeasurementMeta");
        }
    }
}
