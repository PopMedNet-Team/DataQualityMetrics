using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class EntitiesForMetric : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Timestamp",
                table: "Visualizations",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataQualityFrameworkCategories",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Title = table.Column<string>(nullable: true),
                    SubCategory = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityFrameworkCategories", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MetricResultTypes",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricResultTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MetricStatuses",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Access = table.Column<int>(nullable: false),
                    Order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricStatuses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Organization = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Metrics",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    AuthorID = table.Column<Guid>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Justification = table.Column<string>(nullable: true),
                    CDM = table.Column<string>(nullable: true),
                    RelevantTable = table.Column<string>(nullable: true),
                    RelevantColumn = table.Column<string>(nullable: true),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    ModifiedOn = table.Column<DateTime>(nullable: false),
                    ServiceDeskUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrics", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Metrics_Users_AuthorID",
                        column: x => x.AuthorID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metric_DataQualityFrameworkCategories",
                columns: table => new
                {
                    MetricID = table.Column<Guid>(nullable: false),
                    DataQualityFrameworkCategoryID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metric_DataQualityFrameworkCategories", x => new { x.MetricID, x.DataQualityFrameworkCategoryID });
                    table.ForeignKey(
                        name: "FK_Metric_DataQualityFrameworkCategories_DataQualityFrameworkCategories_DataQualityFrameworkCategoryID",
                        column: x => x.DataQualityFrameworkCategoryID,
                        principalTable: "DataQualityFrameworkCategories",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Metric_DataQualityFrameworkCategories_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metric_Domains",
                columns: table => new
                {
                    MetricID = table.Column<Guid>(nullable: false),
                    DomainID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metric_Domains", x => new { x.MetricID, x.DomainID });
                    table.ForeignKey(
                        name: "FK_Metric_Domains_Domains_DomainID",
                        column: x => x.DomainID,
                        principalTable: "Domains",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Metric_Domains_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "MetricStatusItems",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    MetricID = table.Column<Guid>(nullable: false),
                    UserID = table.Column<Guid>(nullable: false),
                    PreviousMetricStatusID = table.Column<Guid>(nullable: true),
                    CreateOn = table.Column<DateTime>(nullable: false),
                    MetricStatusID = table.Column<Guid>(nullable: false),
                    Note = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricStatusItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MetricStatusItems_Metrics_MetricID",
                        column: x => x.MetricID,
                        principalTable: "Metrics",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetricStatusItems_MetricStatuses_MetricStatusID",
                        column: x => x.MetricStatusID,
                        principalTable: "MetricStatuses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetricStatusItems_MetricStatusItems_PreviousMetricStatusID",
                        column: x => x.PreviousMetricStatusID,
                        principalTable: "MetricStatusItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricStatusItems_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Metric_DataQualityFrameworkCategories_DataQualityFrameworkCategoryID",
                table: "Metric_DataQualityFrameworkCategories",
                column: "DataQualityFrameworkCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Metric_Domains_DomainID",
                table: "Metric_Domains",
                column: "DomainID");

            migrationBuilder.CreateIndex(
                name: "IX_Metric_MetricResultsTypes_MetricResultTypeID",
                table: "Metric_MetricResultsTypes",
                column: "MetricResultTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_AuthorID",
                table: "Metrics",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_MetricStatusItems_MetricID",
                table: "MetricStatusItems",
                column: "MetricID");

            migrationBuilder.CreateIndex(
                name: "IX_MetricStatusItems_MetricStatusID",
                table: "MetricStatusItems",
                column: "MetricStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_MetricStatusItems_PreviousMetricStatusID",
                table: "MetricStatusItems",
                column: "PreviousMetricStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_MetricStatusItems_UserID",
                table: "MetricStatusItems",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Metric_DataQualityFrameworkCategories");

            migrationBuilder.DropTable(
                name: "Metric_Domains");

            migrationBuilder.DropTable(
                name: "Metric_MetricResultsTypes");

            migrationBuilder.DropTable(
                name: "MetricStatusItems");

            migrationBuilder.DropTable(
                name: "DataQualityFrameworkCategories");

            migrationBuilder.DropTable(
                name: "Domains");

            migrationBuilder.DropTable(
                name: "MetricResultTypes");

            migrationBuilder.DropTable(
                name: "Metrics");

            migrationBuilder.DropTable(
                name: "MetricStatuses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Visualizations");
        }
    }
}
