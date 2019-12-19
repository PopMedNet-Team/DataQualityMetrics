using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class AddVectorToResultTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO [MetricResultTypes] (ID, [Value]) VALUES ('D6F5C627-45E4-43B4-9A8C-5894074E9498', 'Vector')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [MetricResultTypes] WHERE ID = 'D6F5C627-45E4-43B4-9A8C-5894074E9498'");
        }
    }
}
