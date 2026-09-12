using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitApplicationGuidelines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermitApplicationGuidelines",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitTypeListItemId = table.Column<int>(type: "int", nullable: false),
                    Guidelines = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplicationGuidelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermitApplicationGuidelines_ListItem_PermitTypeListItemId",
                        column: x => x.PermitTypeListItemId,
                        principalSchema: "dbo",
                        principalTable: "ListItem",
                        principalColumn: "ListItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplicationGuidelines_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermitApplicationGuidelines_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplicationGuidelines_CreatedByUserId",
                schema: "dbo",
                table: "PermitApplicationGuidelines",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplicationGuidelines_UpdatedByUserId",
                schema: "dbo",
                table: "PermitApplicationGuidelines",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_PermitApplicationGuidelines_ActivePermitType",
                schema: "dbo",
                table: "PermitApplicationGuidelines",
                column: "PermitTypeListItemId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.Sql("""
                MERGE [dbo].[PermissionPolicy] AS target
                USING (VALUES
                    (N'PermitApplicationGuideline.View',N'View permit application guidelines',N'View and search permit-type guidelines.'),
                    (N'PermitApplicationGuideline.Create',N'Create permit application guidelines',N'Create permit-type guidelines.'),
                    (N'PermitApplicationGuideline.Edit',N'Edit permit application guidelines',N'Edit permit-type guidelines.'),
                    (N'PermitApplicationGuideline.Delete',N'Deactivate permit application guidelines',N'Deactivate permit-type guidelines.')
                ) AS source([Code],[Name],[Description])
                ON target.[Code]=source.[Code]
                WHEN MATCHED THEN UPDATE SET
                    [Name]=source.[Name],[Description]=source.[Description],[ModuleCode]=N'ORGANIZATION',
                    [MenuController]=N'Setup',[MenuAction]=N'PermitApplicationGuidelines',
                    [RequiresMenuAccess]=0,[IsActive]=1,[UpdatedAtUtc]=SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Code],[Name],[Description],[ModuleCode],[MenuController],[MenuAction],
                            [RequiresMenuAccess],[IsActive],[CreatedAtUtc])
                    VALUES (source.[Code],source.[Name],source.[Description],N'ORGANIZATION',N'Setup',
                            N'PermitApplicationGuidelines',0,1,SYSUTCDATETIME());

                INSERT [dbo].[RolePermission]
                    ([RoleId],[PermissionPolicyId],[IsActive],[AssignedAtUtc],[AssignedBy])
                SELECT role.[Id],policy.[Id],1,SYSUTCDATETIME(),N'Permit application guidelines migration'
                FROM [dbo].[Role] role
                CROSS JOIN [dbo].[PermissionPolicy] policy
                WHERE role.[NormalizedName]=N'ADMIN' AND role.[IsActive]=1
                  AND policy.[Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
                      N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete')
                  AND NOT EXISTS
                  (
                      SELECT 1 FROM [dbo].[RolePermission] existing
                      WHERE existing.[RoleId]=role.[Id] AND existing.[PermissionPolicyId]=policy.[Id]
                  );

                UPDATE assignment SET [IsActive]=1,[ModifiedAtUtc]=SYSUTCDATETIME(),
                    [ModifiedBy]=N'Permit application guidelines migration'
                FROM [dbo].[RolePermission] assignment
                INNER JOIN [dbo].[Role] role ON role.[Id]=assignment.[RoleId]
                INNER JOIN [dbo].[PermissionPolicy] policy ON policy.[Id]=assignment.[PermissionPolicyId]
                WHERE role.[NormalizedName]=N'ADMIN' AND role.[IsActive]=1
                  AND assignment.[IsActive]=0
                  AND policy.[Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
                      N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE assignment
                FROM [dbo].[RolePermission] assignment
                INNER JOIN [dbo].[PermissionPolicy] policy ON policy.[Id]=assignment.[PermissionPolicyId]
                WHERE policy.[Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
                    N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete');
                DELETE FROM [dbo].[PermissionPolicy]
                WHERE [Code] IN (N'PermitApplicationGuideline.View',N'PermitApplicationGuideline.Create',
                    N'PermitApplicationGuideline.Edit',N'PermitApplicationGuideline.Delete');
                """);

            migrationBuilder.DropTable(
                name: "PermitApplicationGuidelines",
                schema: "dbo");
        }
    }
}
