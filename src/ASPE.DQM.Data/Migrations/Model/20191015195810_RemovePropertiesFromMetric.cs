using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class RemovePropertiesFromMetric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CDM",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "Network_Project",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "RelevantColumn",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "RelevantTable",
                table: "Metrics");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CDM",
                table: "Metrics",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Network_Project",
                table: "Metrics",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelevantColumn",
                table: "Metrics",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelevantTable",
                table: "Metrics",
                nullable: true);
        }
    }
}
