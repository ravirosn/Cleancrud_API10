using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesBranchesAndDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "OfficeBranch",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsHeadOffice = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficeBranchId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_OfficeBranch_OfficeBranchId",
                        column: x => x.OfficeBranchId,
                        principalSchema: "dbo",
                        principalTable: "OfficeBranch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRole_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [dbo].[Role] ([Name], [NormalizedName], [IsActive], [CreatedAtUtc])
                VALUES (N'User', N'USER', 1, SYSUTCDATETIME()),
                       (N'Admin', N'ADMIN', 1, SYSUTCDATETIME());

                INSERT INTO [dbo].[Role] ([Name], [NormalizedName], [IsActive], [CreatedAtUtc])
                SELECT MIN(LTRIM(RTRIM([Role]))), UPPER(LTRIM(RTRIM([Role]))), 1, SYSUTCDATETIME()
                FROM [Users]
                WHERE LTRIM(RTRIM([Role])) <> N''
                  AND UPPER(LTRIM(RTRIM([Role]))) NOT IN (N'USER', N'ADMIN')
                GROUP BY UPPER(LTRIM(RTRIM([Role])));

                INSERT INTO [dbo].[UserRole] ([UserId], [RoleId], [IsActive], [AssignedAtUtc])
                SELECT u.[Id], r.[Id], 1, SYSUTCDATETIME()
                FROM [Users] u
                INNER JOIN [dbo].[Role] r
                    ON r.[NormalizedName] = COALESCE(
                        NULLIF(UPPER(LTRIM(RTRIM(u.[Role]))), N''), N'USER');
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OfficeBranch_HeadOfficeActive",
                schema: "dbo",
                table: "OfficeBranch",
                sql: "[IsHeadOffice] = 0 OR [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Department_OfficeBranchId_Code",
                schema: "dbo",
                table: "Department",
                columns: new[] { "OfficeBranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_OfficeBranchId_IsActive",
                schema: "dbo",
                table: "Department",
                columns: new[] { "OfficeBranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficeBranch_Code",
                schema: "dbo",
                table: "OfficeBranch",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficeBranch_IsHeadOffice",
                schema: "dbo",
                table: "OfficeBranch",
                column: "IsHeadOffice",
                unique: true,
                filter: "[IsHeadOffice] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Role_NormalizedName",
                schema: "dbo",
                table: "Role",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId_IsActive",
                schema: "dbo",
                table: "UserRole",
                columns: new[] { "RoleId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.Sql("""
                UPDATE u
                SET u.[Role] = selectedRole.[Name]
                FROM [Users] u
                CROSS APPLY (
                    SELECT TOP (1) r.[Name]
                    FROM [dbo].[UserRole] ur
                    INNER JOIN [dbo].[Role] r ON r.[Id] = ur.[RoleId]
                    WHERE ur.[UserId] = u.[Id] AND ur.[IsActive] = 1 AND r.[IsActive] = 1
                    ORDER BY CASE WHEN r.[NormalizedName] = N'ADMIN' THEN 0 ELSE 1 END, r.[Id]
                ) selectedRole;
                """);

            migrationBuilder.DropTable(
                name: "Department",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OfficeBranch",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "dbo");

        }
    }
}
