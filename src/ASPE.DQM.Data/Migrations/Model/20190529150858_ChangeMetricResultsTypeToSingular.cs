using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class ChangeMetricResultsTypeToSingular : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Metric_MetricResultsTypes");

            migrationBuilder.AddColumn<string>(
                name: "Network_Project",
                table: "Metrics",
                nullable: true,
                maxLength:500);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultsTypeID",
                table: "Metrics",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_ResultsTypeID",
                table: "Metrics",
                column: "ResultsTypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Metrics_MetricResultTypes_ResultsTypeID",
                table: "Metrics",
                column: "ResultsTypeID",
                principalTable: "MetricResultTypes",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Metrics_MetricResultTypes_ResultsTypeID",
                table: "Metrics");

            migrationBuilder.DropIndex(
                name: "IX_Metrics_ResultsTypeID",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "Network_Project",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "ResultsTypeID",
                table: "Metrics");

            migrationBuilder.CreateTable(
                name: "Metric_MetricResultsTypes",
                columns: table => new
                {
                    MetricID = table.Column<Guid>(nullable: false),
                    MetricResultTypeID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metric_MetricResultsTypes", x => new { x.MetricID, x.MetricResultTypeID });
                    table.ForeignKey(
                        name: "FK_Metric_MetricResultsTypes_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Metric_MetricResultsTypes_MetricResultTypes_MetricResultTypeID",
                        column: x => x.MetricResultTypeID,
                        principalTable: "MetricResultTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Metric_MetricResultsTypes_MetricResultTypeID",
                table: "Metric_MetricResultsTypes",
                column: "MetricResultTypeID");
        }
    }
}
