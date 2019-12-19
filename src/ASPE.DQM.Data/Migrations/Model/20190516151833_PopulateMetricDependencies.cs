using Microsoft.EntityFrameworkCore.Migrations;

namespace ASPE.DQM.Migrations.Model
{
    public partial class PopulateMetricDependencies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>("Title", "Domains", nullable: false, maxLength: 200);
            migrationBuilder.CreateIndex("IX_Domains_Title", "Domains", "Title", unique: true);

            migrationBuilder.AlterColumn<string>("Title", "DataQualityFrameworkCategories", nullable: false, maxLength: 80);
            migrationBuilder.AlterColumn<string>("SubCategory", "DataQualityFrameworkCategories", nullable: true, maxLength: 100);
            migrationBuilder.CreateIndex("IX_DataQualityFrameworkCategories_Title_SubCategory", "DataQualityFrameworkCategories", new[] { "Title", "SubCategory" }, unique: true);

            migrationBuilder.AlterColumn<string>("Value", "MetricResultTypes", nullable: false, maxLength: 80);
            migrationBuilder.CreateIndex("IX_MetricResultTypes_Value", "MetricResultTypes", "Value", unique: true);

            migrationBuilder.AlterColumn<string>("Title", "MetricStatuses", nullable: false, maxLength: 120);
            migrationBuilder.CreateIndex("IX_MetricStatuses", "MetricStatuses", new[] { "Title", "Order" }, unique: true);

            migrationBuilder.AlterColumn<string>("Title", "Metrics", nullable: false, maxLength: 500);
            migrationBuilder.AlterColumn<string>("CDM", "Metrics", nullable: false, maxLength: 200);
            migrationBuilder.AlterColumn<string>("RelevantTable", "Metrics", nullable: false, maxLength: 200);
            migrationBuilder.AlterColumn<string>("RelevantColumn", "Metrics", nullable: false, maxLength: 200);
            migrationBuilder.AlterColumn<string>("ServiceDeskUrl", "Metrics", nullable: false, maxLength: 500);

            migrationBuilder.AlterColumn<string>("UserName", "Users", nullable: false, maxLength: 256);            
            migrationBuilder.AlterColumn<string>("FirstName", "Users", nullable: true, maxLength: 256);
            migrationBuilder.AlterColumn<string>("LastName", "Users", nullable: false, maxLength: 256);
            migrationBuilder.AlterColumn<string>("Organization", "Users", nullable: false, maxLength: 256);
            migrationBuilder.AlterColumn<string>("Email", "Users", nullable: false, maxLength: 256);
            migrationBuilder.AlterColumn<string>("PhoneNumber", "Users", nullable: false, maxLength: 80);

            migrationBuilder.AlterColumn<string>("Title", "Visualizations", nullable: false, maxLength: 256);

            migrationBuilder.Sql("INSERT INTO [Domains] (ID, Title) VALUES ('1b090de0-895c-4a71-a268-aa4f00b9cb96', 'Demographics'), ('daae8071-6257-41bc-b074-aa4f00b9cb96', 'Medications'), ('a91f4432-db70-4446-8306-aa4f00b9cb96', 'Procedures'), ('2ac804e7-b6ef-4fcb-ac6f-aa4f00b9cb96', 'Diagnoses'), ('c96d1aff-c455-41a6-822b-aa4f00b9cb96', 'Vitals'), ('afba9785-0e40-4263-ba76-aa4f00b9cb96', 'Enrollment'), ('2b258737-c9be-4961-afbd-aa4f00b9cb96', 'Encounters'), ('8e505805-29b2-4a70-93f1-aa4f00b9cb96', 'Labs'), ('2d502627-4f49-47fc-a36f-aa4f00b9cb96', 'Social determinants of health')");
            migrationBuilder.Sql("INSERT INTO [DataQualityFrameworkCategories] (ID, Title, SubCategory) VALUES ('0c34d7be-b97c-4b0a-aff8-aa4f00b9cb96', 'Conformance', 'value'), ('5f853446-2d3a-433e-8363-aa4f00b9cb96', 'Conformance', 'relational'), ('a7674b97-cc9c-430d-a77b-aa4f00b9cb96', 'Conformance', 'computational'), ('279e39f2-6052-49a4-ae92-aa4f00b9cb96', 'Completeness', ''), ('d648a7ae-52bf-4d2b-887a-aa4f00b9cb96', 'Plausibility', 'uniqueness'), ('76fb11a9-8437-499d-b8d0-aa4f00b9cb96', 'Plausibility', 'atemporal'), ('0a11dece-23fe-4d7f-8833-aa4f00b9cb96', 'Plausibility', 'temporal')");
            migrationBuilder.Sql("INSERT INTO [MetricResultTypes] (ID, [Value]) VALUES ('6b2d1295-f306-4e4b-94df-aa4f00b9cb96', 'Count'), ('84b7d4b6-f939-43ce-9ca1-aa4f00b9cb96', 'Percentage'), ('72b755f7-d7fa-4025-927e-aa4f00b9cb96', 'Range')");
            migrationBuilder.Sql("INSERT INTO [MetricStatuses] (ID, Title, Access, [Order]) VALUES ('af5892ea-807c-4f1d-9989-aa4f00b9cb96', 'Draft', 1, 0), ('91bff71d-6e3b-4d5a-8947-aa4f00b9cb96', 'Submitted', 3, 5), ('e7d3591c-d912-42c6-88e2-aa4f00b9cb96', 'In Review', 3, 10), ('3ce548a3-4e91-4fe0-9d70-aa4f00b9cb96', 'Published', 8, 15), ('a56e66ba-6088-49df-9247-aa4f00b9cb96', 'Published - Requires Authentication', 4, 16), ('546e8d36-4979-449a-b730-aa4f00b9cb96', 'Rejected', 3, 20), ('ac70e2a2-9c22-4d1e-b378-aa4f00b9cb96', 'Inactive', 2, 100), ('0b930582-060a-4fe8-ab50-aa4f00b9cb96', 'Deleted', 2, 1000)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_Domains_Title", "Domains");
            migrationBuilder.DropIndex("IX_DataQualityFrameworkCategories_Title_SubCategory", "DataQualityFrameworkCategories");
            migrationBuilder.DropIndex("IX_MetricResultTypes_Value", "MetricResultTypes");
            migrationBuilder.DropIndex("IX_MetricStatuses", "MetricStatuses");

            migrationBuilder.Sql("DELETE FROM [Domains]");
            migrationBuilder.Sql("DELETE FROM [DataQualityFrameworkCategories]");
            migrationBuilder.Sql("DELETE FROM [MetricResultTypes]");
            migrationBuilder.Sql("DELETE FROM [MetricStatuses]");
        }
    }
}
