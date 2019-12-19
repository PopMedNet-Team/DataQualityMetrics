using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class User_Metric_And_Visualization_Favorites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User_MetricFavorites",
                columns: table => new
                {
                    UserID = table.Column<Guid>(nullable: false),
                    MetricID = table.Column<Guid>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_MetricFavorites", x => new { x.UserID, x.MetricID });
                    table.ForeignKey(
                        name: "FK_User_MetricFavorites_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_User_MetricFavorites_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "User_VisualizationFavorites",
                columns: table => new
                {
                    UserID = table.Column<Guid>(nullable: false),
                    VisualizationID = table.Column<Guid>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_VisualizationFavorites", x => new { x.UserID, x.VisualizationID });
                    table.ForeignKey(
                        name: "FK_User_VisualizationFavorites_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_User_VisualizationFavorites_Visualizations_VisualizationID",
                        column: x => x.VisualizationID,
                        principalTable: "Visualizations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_MetricFavorites_MetricID",
                table: "User_MetricFavorites",
                column: "MetricID");

            migrationBuilder.CreateIndex(
                name: "IX_User_VisualizationFavorites_VisualizationID",
                table: "User_VisualizationFavorites",
                column: "VisualizationID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User_MetricFavorites");

            migrationBuilder.DropTable(
                name: "User_VisualizationFavorites");
        }
    }
}
