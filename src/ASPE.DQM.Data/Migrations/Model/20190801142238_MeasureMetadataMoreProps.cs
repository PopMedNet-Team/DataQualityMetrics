using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class MeasureMetadataMoreProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeasurementMeta_Metrics_MetricID",
                table: "MeasurementMeta");

            migrationBuilder.AddColumn<string>(
                name: "CommonDataModelVersion",
                table: "MeasurementMeta",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluatedVariableDataType",
                table: "MeasurementMeta",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultsDelimiter",
                table: "MeasurementMeta",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MeasurementMeta_Metrics_MetricID",
                table: "MeasurementMeta",
                column: "MetricID",
                principalTable: "Metrics",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeasurementMeta_Metrics_MetricID",
                table: "MeasurementMeta");

            migrationBuilder.DropColumn(
                name: "CommonDataModelVersion",
                table: "MeasurementMeta");

            migrationBuilder.DropColumn(
                name: "EvaluatedVariableDataType",
                table: "MeasurementMeta");

            migrationBuilder.DropColumn(
                name: "ResultsDelimiter",
                table: "MeasurementMeta");

            migrationBuilder.AddForeignKey(
                name: "FK_MeasurementMeta_Metrics_MetricID",
                table: "MeasurementMeta",
                column: "MetricID",
                principalTable: "Metrics",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
