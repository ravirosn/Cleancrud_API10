using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleModuleMenuAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_ModuleMenu_ApplicationModuleId_Id",
                schema: "dbo",
                table: "ModuleMenu",
                columns: new[] { "ApplicationModuleId", "Id" });

            migrationBuilder.CreateTable(
                name: "RoleModuleMenu",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ApplicationModuleId = table.Column<int>(type: "int", nullable: false),
                    ModuleMenuId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleModuleMenu", x => new { x.RoleId, x.ApplicationModuleId, x.ModuleMenuId });
                    table.ForeignKey(
                        name: "FK_RoleModuleMenu_ModuleMenu_ApplicationModuleId_ModuleMenuId",
                        columns: x => new { x.ApplicationModuleId, x.ModuleMenuId },
                        principalSchema: "dbo",
                        principalTable: "ModuleMenu",
                        principalColumns: new[] { "ApplicationModuleId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleModuleMenu_RoleModule_RoleId_ApplicationModuleId",
                        columns: x => new { x.RoleId, x.ApplicationModuleId },
                        principalSchema: "dbo",
                        principalTable: "RoleModule",
                        principalColumns: new[] { "RoleId", "ApplicationModuleId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleModuleMenu_ApplicationModuleId_ModuleMenuId_IsActive",
                schema: "dbo",
                table: "RoleModuleMenu",
                columns: new[] { "ApplicationModuleId", "ModuleMenuId", "IsActive" });

            // Preserve the navigation currently available through existing role-module grants.
            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[RoleModuleMenu]
                    ([RoleId], [ApplicationModuleId], [ModuleMenuId], [IsActive], [AssignedAtUtc])
                SELECT rm.[RoleId], rm.[ApplicationModuleId], mm.[Id], CAST(1 AS bit), SYSUTCDATETIME()
                FROM [dbo].[RoleModule] rm
                INNER JOIN [dbo].[ModuleMenu] mm
                    ON mm.[ApplicationModuleId] = rm.[ApplicationModuleId]
                WHERE rm.[IsActive] = 1 AND mm.[IsActive] = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleModuleMenu",
                schema: "dbo");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ModuleMenu_ApplicationModuleId_Id",
                schema: "dbo",
                table: "ModuleMenu");
        }
    }
}
