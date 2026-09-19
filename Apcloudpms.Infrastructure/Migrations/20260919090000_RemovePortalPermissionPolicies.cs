using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260919090000_RemovePortalPermissionPolicies")]
public sealed class RemovePortalPermissionPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DELETE assignment
        FROM dbo.RolePermission assignment
        INNER JOIN dbo.PermissionPolicy policy
            ON policy.Id = assignment.PermissionPolicyId
        WHERE policy.Code IN (N'Portal.View', N'Portal.SelectModule');

        DELETE FROM dbo.PermissionPolicy
        WHERE Code IN (N'Portal.View', N'Portal.SelectModule');
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        MERGE dbo.PermissionPolicy AS target
        USING (VALUES
          (N'Portal.View',N'Open portal',N'Open the application portal.',N'PORTAL',N'Portal',N'Index'),
          (N'Portal.SelectModule',N'Select portal modules',N'Open a module from the portal.',N'PORTAL',N'Portal',N'Module')
        ) AS source(Code,Name,Description,ModuleCode,MenuController,MenuAction)
        ON target.Code = source.Code
        WHEN MATCHED THEN UPDATE SET
          Name = source.Name,
          Description = source.Description,
          ModuleCode = source.ModuleCode,
          MenuController = source.MenuController,
          MenuAction = source.MenuAction,
          RequiresMenuAccess = 0,
          IsActive = 1,
          UpdatedAtUtc = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT
          (Code,Name,Description,ModuleCode,MenuController,MenuAction,
           RequiresMenuAccess,IsActive,CreatedAtUtc)
        VALUES
          (source.Code,source.Name,source.Description,source.ModuleCode,
           source.MenuController,source.MenuAction,0,1,SYSUTCDATETIME());
        """);
}
