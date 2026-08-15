using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationModulesAndNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationModule",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationModule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleMenu",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationModuleId = table.Column<int>(type: "int", nullable: false),
                    ParentMenuId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QueryUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleMenu_ApplicationModule_ApplicationModuleId",
                        column: x => x.ApplicationModuleId,
                        principalSchema: "dbo",
                        principalTable: "ApplicationModule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleMenu_ModuleMenu_ParentMenuId",
                        column: x => x.ParentMenuId,
                        principalSchema: "dbo",
                        principalTable: "ModuleMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserModule",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ApplicationModuleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModule", x => new { x.UserId, x.ApplicationModuleId });
                    table.ForeignKey(
                        name: "FK_UserModule_ApplicationModule_ApplicationModuleId",
                        column: x => x.ApplicationModuleId,
                        principalSchema: "dbo",
                        principalTable: "ApplicationModule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserModule_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ApplicationModule",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "DisplayOrder", "Icon", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "PERMIT", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Manage permit applications, reviews, and approvals.", 1, "file-check", true, "Permit Management System" },
                    { 2, "VISITOR", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Manage visitor registration, check-in, and visit history.", 2, "users", true, "Visitor Management System" },
                    { 3, "ASSET", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Manage organizational assets and assignments.", 3, "package", true, "Asset Management System" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ModuleMenu",
                columns: new[] { "Id", "ActionName", "ApplicationModuleId", "ControllerName", "CreatedAtUtc", "DisplayOrder", "Icon", "IsActive", "Name", "ParentMenuId", "QueryUrl" },
                values: new object[,]
                {
                    { 1, "Index", 1, "PermitDashboard", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1, "dashboard", true, "Dashboard", null, "/api/permit/dashboard" },
                    { 2, "Index", 1, "PermitApplications", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, "file-text", true, "Permit Applications", null, "/api/permit/applications" },
                    { 3, "Index", 1, "PermitApprovals", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 3, "check-circle", true, "Permit Approvals", null, "/api/permit/approvals" },
                    { 4, "Index", 2, "VisitorDashboard", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1, "dashboard", true, "Dashboard", null, "/api/visitor/dashboard" },
                    { 5, "Index", 2, "VisitorCheckIn", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, "log-in", true, "Visitor Check-In", null, "/api/visitor/check-in" },
                    { 6, "Index", 2, "VisitorLog", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 3, "list", true, "Visitor Log", null, "/api/visitor/log" },
                    { 7, "Index", 3, "AssetDashboard", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1, "dashboard", true, "Dashboard", null, "/api/asset/dashboard" },
                    { 8, "Index", 3, "AssetRegister", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, "archive", true, "Asset Register", null, "/api/asset/register" },
                    { 9, "Index", 3, "AssetAssignments", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), 3, "user-check", true, "Asset Assignments", null, "/api/asset/assignments" }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[UserModule]
                    ([UserId], [ApplicationModuleId], [IsActive], [AssignedAtUtc])
                SELECT u.[Id], m.[Id], 1, SYSUTCDATETIME()
                FROM [Users] u
                CROSS JOIN [dbo].[ApplicationModule] m
                WHERE u.[IsActive] = 1
                  AND (
                      m.[Id] IN (1, 2)
                      OR EXISTS (
                          SELECT 1
                          FROM [dbo].[UserRole] ur
                          INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
                          WHERE ur.[UserId] = u.[Id]
                            AND ur.[IsActive] = 1
                            AND r.[IsActive] = 1
                            AND r.[NormalizedName] = N'ADMIN'
                      )
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationModule_Code",
                schema: "dbo",
                table: "ApplicationModule",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationModule_IsActive_DisplayOrder",
                schema: "dbo",
                table: "ApplicationModule",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMenu_ApplicationModuleId_IsActive_DisplayOrder",
                schema: "dbo",
                table: "ModuleMenu",
                columns: new[] { "ApplicationModuleId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMenu_ApplicationModuleId_QueryUrl",
                schema: "dbo",
                table: "ModuleMenu",
                columns: new[] { "ApplicationModuleId", "QueryUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMenu_ParentMenuId",
                schema: "dbo",
                table: "ModuleMenu",
                column: "ParentMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModule_ApplicationModuleId_IsActive",
                schema: "dbo",
                table: "UserModule",
                columns: new[] { "ApplicationModuleId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleMenu",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserModule",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ApplicationModule",
                schema: "dbo");
        }
    }
}
