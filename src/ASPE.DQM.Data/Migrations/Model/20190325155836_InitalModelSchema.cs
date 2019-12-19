using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class InitalModelSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Visualizations",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    AppID = table.Column<string>(nullable: true),
                    RequiresAuth = table.Column<bool>(nullable: false),
                    Published = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visualizations", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visualizations");
        }
    }
}
