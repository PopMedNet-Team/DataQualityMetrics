using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class AddObjectIDToVisualRegistration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SheetID",
                table: "Visualizations",
                maxLength:50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SheetID",
                table: "Visualizations");
        }
    }
}
