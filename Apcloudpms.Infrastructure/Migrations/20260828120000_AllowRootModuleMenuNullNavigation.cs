using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828120000_AllowRootModuleMenuNullNavigation")]
public partial class AllowRootModuleMenuNullNavigation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ModuleMenu_ApplicationModuleId_QueryUrl",
            schema: "dbo",
            table: "ModuleMenu");

        migrationBuilder.AlterColumn<string>(
            name: "ControllerName",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "ActionName",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "QueryUrl",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500);

        migrationBuilder.CreateIndex(
            name: "IX_ModuleMenu_ApplicationModuleId_QueryUrl",
            schema: "dbo",
            table: "ModuleMenu",
            columns: new[] { "ApplicationModuleId", "QueryUrl" },
            unique: true,
            filter: "[QueryUrl] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ModuleMenu_ApplicationModuleId_QueryUrl",
            schema: "dbo",
            table: "ModuleMenu");

        migrationBuilder.Sql("""
            UPDATE dbo.ModuleMenu
            SET ControllerName = COALESCE(ControllerName, N''),
                ActionName = COALESCE(ActionName, N''),
                QueryUrl = COALESCE(QueryUrl, N'/menu/' + CONVERT(nvarchar(20), Id));
            """);

        migrationBuilder.AlterColumn<string>(
            name: "ControllerName",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "ActionName",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "QueryUrl",
            schema: "dbo",
            table: "ModuleMenu",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ModuleMenu_ApplicationModuleId_QueryUrl",
            schema: "dbo",
            table: "ModuleMenu",
            columns: new[] { "ApplicationModuleId", "QueryUrl" },
            unique: true);
    }
}
