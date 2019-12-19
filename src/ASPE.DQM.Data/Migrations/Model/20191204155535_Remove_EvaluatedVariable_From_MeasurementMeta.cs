using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class Remove_EvaluatedVariable_From_MeasurementMeta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvaluatedVariableDataType",
                table: "MeasurementMeta");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvaluatedVariableDataType",
                table: "MeasurementMeta",
                nullable: true);
        }
    }
}
