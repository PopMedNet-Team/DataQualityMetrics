using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class AllowNullInUsersTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>("PhoneNumber", "Users", nullable: true, maxLength:80);
            migrationBuilder.AlterColumn<string>("LastName", "Users", nullable: true, maxLength:256);
            migrationBuilder.AlterColumn<string>("Organization", "Users", nullable: true, maxLength:256);
            migrationBuilder.AlterColumn<string>("Email", "Users", nullable: true, maxLength:256);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>("PhoneNumber", "Users", nullable: false, maxLength:80);
            migrationBuilder.AlterColumn<string>("LastName", "Users", nullable: false, maxLength:256);
            migrationBuilder.AlterColumn<string>("Organization", "Users", nullable: false, maxLength:256);
            migrationBuilder.AlterColumn<string>("Email", "Users", nullable: false, maxLength:256);
        }
    }
}
