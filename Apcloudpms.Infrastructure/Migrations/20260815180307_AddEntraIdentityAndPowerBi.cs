using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraIdentityAndPowerBi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntraObjectId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EntraTenantId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ApplicationModule",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "DisplayOrder", "Icon", "IsActive", "Name" },
                values: new object[] { 4, "POWERBI", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), "View embedded Power BI dashboards and reports.", 4, "bar-chart", true, "Analytics and Reports" });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ModuleMenu",
                columns: new[] { "Id", "ActionName", "ApplicationModuleId", "ControllerName", "CreatedAtUtc", "DisplayOrder", "Icon", "IsActive", "Name", "ParentMenuId", "QueryUrl" },
                values: new object[] { 10, "GetEmbedConfig", 4, "PowerBi", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1, "bar-chart-2", true, "Power BI Report", null, "/api/power-bi/embed-config" });

            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[UserModule]
                    ([UserId], [ApplicationModuleId], [IsActive], [AssignedAtUtc])
                SELECT DISTINCT ur.[UserId], 4, 1, SYSUTCDATETIME()
                FROM [dbo].[UserRole] ur
                INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
                INNER JOIN [Users] u ON u.[Id] = ur.[UserId]
                WHERE ur.[IsActive] = 1
                  AND r.[IsActive] = 1
                  AND r.[NormalizedName] = N'ADMIN'
                  AND u.[IsActive] = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[UserModule] um
                      WHERE um.[UserId] = ur.[UserId]
                        AND um.[ApplicationModuleId] = 4
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntraTenantId_EntraObjectId",
                table: "Users",
                columns: new[] { "EntraTenantId", "EntraObjectId" },
                unique: true,
                filter: "[EntraTenantId] IS NOT NULL AND [EntraObjectId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM [dbo].[UserModule] WHERE [ApplicationModuleId] = 4;");

            migrationBuilder.DropIndex(
                name: "IX_Users_EntraTenantId_EntraObjectId",
                table: "Users");

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "ModuleMenu",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "ApplicationModule",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EntraObjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EntraTenantId",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
