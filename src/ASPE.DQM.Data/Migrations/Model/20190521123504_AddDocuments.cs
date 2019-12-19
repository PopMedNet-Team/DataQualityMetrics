using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class AddDocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Name = table.Column<string>(nullable: false, maxLength: 255),
                    FileName = table.Column<string>(nullable: false, maxLength: 255),
                    isViewable = table.Column<bool>(nullable: false),
                    MimeType = table.Column<string>(nullable: true, maxLength: 100),
                    Kind = table.Column<string>(nullable: true, maxLength: 50),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    ContentModifiedOn = table.Column<DateTime>(nullable: true),
                    ContentCreatedOn = table.Column<DateTime>(nullable: true),
                    Length = table.Column<long>(nullable: false),
                    ItemID = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ParentDocumentID = table.Column<Guid>(nullable: true),
                    UploadedByID = table.Column<Guid>(nullable: true),
                    RevisionSetID = table.Column<Guid>(nullable: true),
                    RevisionDescription = table.Column<string>(nullable: true),
                    MajorVersion = table.Column<int>(nullable: false),
                    MinorVersion = table.Column<int>(nullable: false),
                    BuildVersion = table.Column<int>(nullable: false),
                    RevisionVersion = table.Column<int>(nullable: false),
                    ChunkCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_ParentDocumentID",
                        column: x => x.ParentDocumentID,
                        principalTable: "Documents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Users_UploadedByID",
                        column: x => x.UploadedByID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileName",
                table: "Documents",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Name",
                table: "Documents",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ParentDocumentID",
                table: "Documents",
                column: "ParentDocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByID",
                table: "Documents",
                column: "UploadedByID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
