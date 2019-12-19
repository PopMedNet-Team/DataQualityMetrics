using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class AddEditableFlagToMetricStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowEdit",
                table: "MetricStatuses",
                nullable: false,
                defaultValue: false);

            //by default the metric is not editable, update Draft, 
            migrationBuilder.Sql("UPDATE MetricStatuses SET AllowEdit = 1 WHERE ID IN ('AF5892EA-807C-4F1D-9989-AA4F00B9CB96', 'E7D3591C-D912-42C6-88E2-AA4F00B9CB96', '546E8D36-4979-449A-B730-AA4F00B9CB96')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowEdit",
                table: "MetricStatuses");
        }
    }
}
