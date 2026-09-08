using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkPermissionAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                MERGE dbo.PermissionPolicy AS target
                USING (VALUES (
                  N'PermissionAssignment.Manage',
                  N'Bulk manage permission assignments',
                  N'Replace a role''s active permission assignments in one operation.',
                  N'ORGANIZATION',N'Setup',N'Permission',CONVERT(bit,1)))
                  AS source(Code,Name,Description,ModuleCode,MenuController,MenuAction,RequiresMenuAccess)
                ON target.Code=source.Code
                WHEN MATCHED THEN UPDATE SET Name=source.Name,Description=source.Description,
                  ModuleCode=source.ModuleCode,MenuController=source.MenuController,
                  MenuAction=source.MenuAction,RequiresMenuAccess=source.RequiresMenuAccess,
                  IsActive=1,UpdatedAtUtc=SYSUTCDATETIME()
                WHEN NOT MATCHED THEN INSERT(
                  Code,Name,Description,ModuleCode,MenuController,MenuAction,
                  RequiresMenuAccess,IsActive,CreatedAtUtc)
                  VALUES(source.Code,source.Name,source.Description,source.ModuleCode,
                    source.MenuController,source.MenuAction,source.RequiresMenuAccess,1,SYSUTCDATETIME());

                INSERT dbo.RolePermission(
                  RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy)
                SELECT role.Id,policy.Id,1,SYSUTCDATETIME(),N'System permission catalog migration'
                FROM dbo.Role role
                CROSS JOIN dbo.PermissionPolicy policy
                WHERE role.NormalizedName=N'ADMIN' AND role.IsActive=1
                  AND policy.Code=N'PermissionAssignment.Manage'
                  AND NOT EXISTS(
                    SELECT 1 FROM dbo.RolePermission assignment
                    WHERE assignment.RoleId=role.Id AND assignment.PermissionPolicyId=policy.Id);

                UPDATE assignment SET IsActive=1,ModifiedAtUtc=SYSUTCDATETIME(),
                  ModifiedBy=N'System permission catalog migration'
                FROM dbo.RolePermission assignment
                JOIN dbo.Role role ON role.Id=assignment.RoleId
                JOIN dbo.PermissionPolicy policy ON policy.Id=assignment.PermissionPolicyId
                WHERE role.NormalizedName=N'ADMIN' AND role.IsActive=1
                  AND policy.Code=N'PermissionAssignment.Manage' AND assignment.IsActive=0;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentsBulkSet
                  @RoleId int,
                  @PermissionPolicyIdsJson nvarchar(max),
                  @ActorUserId int=NULL,
                  @ActorName nvarchar(256)=NULL,
                  @TraceId nvarchar(100)=NULL,
                  @IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                  SET NOCOUNT ON;
                  SET XACT_ABORT ON;

                  IF NOT EXISTS(SELECT 1 FROM dbo.Role WHERE Id=@RoleId AND IsActive=1)
                    THROW 50310,'Active role was not found.',1;
                  IF EXISTS(SELECT 1 FROM dbo.Role WHERE Id=@RoleId AND NormalizedName=N'SUPERADMIN')
                    THROW 50315,'SuperAdmin has global access and does not accept permission assignments.',1;
                  IF ISJSON(@PermissionPolicyIdsJson)<>1
                    THROW 50316,'Permission policy selection is not valid JSON.',1;

                  DECLARE @Selected table(PermissionPolicyId int NOT NULL PRIMARY KEY);
                  INSERT @Selected(PermissionPolicyId)
                  SELECT DISTINCT TRY_CONVERT(int,[value])
                  FROM OPENJSON(@PermissionPolicyIdsJson)
                  WHERE TRY_CONVERT(int,[value]) IS NOT NULL;

                  IF EXISTS(
                    SELECT 1 FROM @Selected selected
                    LEFT JOIN dbo.PermissionPolicy policy ON policy.Id=selected.PermissionPolicyId
                    WHERE selected.PermissionPolicyId<=0 OR policy.Id IS NULL OR policy.IsActive=0)
                    THROW 50311,'One or more active permission policies were not found.',1;

                  -- An already-active orphaned permission may remain selected so an unrelated
                  -- bulk edit is not blocked; it remains ineffective until its menu is restored.
                  -- A new or inactive assignment cannot be activated without its required menu.
                  IF EXISTS(
                    SELECT 1
                    FROM @Selected selected
                    JOIN dbo.PermissionPolicy policy ON policy.Id=selected.PermissionPolicyId
                    WHERE policy.RequiresMenuAccess=1
                      AND NOT EXISTS(
                        SELECT 1 FROM dbo.RolePermission existing
                        WHERE existing.RoleId=@RoleId
                          AND existing.PermissionPolicyId=selected.PermissionPolicyId
                          AND existing.IsActive=1)
                      AND NOT EXISTS(
                        SELECT 1
                        FROM dbo.ApplicationModule module
                        JOIN dbo.RoleModule roleModule
                          ON roleModule.ApplicationModuleId=module.Id
                          AND roleModule.RoleId=@RoleId AND roleModule.IsActive=1
                        JOIN dbo.ModuleMenu menu
                          ON menu.ApplicationModuleId=module.Id
                          AND menu.ControllerName=policy.MenuController
                          AND menu.ActionName=policy.MenuAction AND menu.IsActive=1
                        JOIN dbo.RoleModuleMenu roleMenu
                          ON roleMenu.RoleId=@RoleId
                          AND roleMenu.ApplicationModuleId=module.Id
                          AND roleMenu.ModuleMenuId=menu.Id AND roleMenu.IsActive=1
                        WHERE module.Code=policy.ModuleCode AND module.IsActive=1))
                    THROW 50312,'Assign the required module menus before granting the selected permissions.',1;

                  BEGIN TRANSACTION;
                  DECLARE @Now datetime2(0)=SYSUTCDATETIME();
                  DECLARE @Old nvarchar(max)=(
                    SELECT PermissionPolicyId
                    FROM dbo.RolePermission
                    WHERE RoleId=@RoleId AND IsActive=1
                    ORDER BY PermissionPolicyId FOR JSON PATH);
                  DECLARE @Changed bit=0;

                  UPDATE assignment SET
                    IsActive=CASE WHEN selected.PermissionPolicyId IS NULL THEN 0 ELSE 1 END,
                    ModifiedAtUtc=@Now,ModifiedByUserId=@ActorUserId,ModifiedBy=@ActorName
                  FROM dbo.RolePermission assignment
                  LEFT JOIN @Selected selected
                    ON selected.PermissionPolicyId=assignment.PermissionPolicyId
                  WHERE assignment.RoleId=@RoleId
                    AND assignment.IsActive<>CASE WHEN selected.PermissionPolicyId IS NULL THEN 0 ELSE 1 END;
                  IF @@ROWCOUNT>0 SET @Changed=1;

                  INSERT dbo.RolePermission(
                    RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedByUserId,AssignedBy)
                  SELECT @RoleId,selected.PermissionPolicyId,1,@Now,@ActorUserId,@ActorName
                  FROM @Selected selected
                  WHERE NOT EXISTS(
                    SELECT 1 FROM dbo.RolePermission assignment
                    WHERE assignment.RoleId=@RoleId
                      AND assignment.PermissionPolicyId=selected.PermissionPolicyId);
                  IF @@ROWCOUNT>0 SET @Changed=1;

                  IF @Changed=1
                  BEGIN
                    DECLARE @New nvarchar(max)=(
                      SELECT PermissionPolicyId
                      FROM dbo.RolePermission
                      WHERE RoleId=@RoleId AND IsActive=1
                      ORDER BY PermissionPolicyId FOR JSON PATH);
                    INSERT dbo.AuditLog(
                      EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,
                      ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc)
                    VALUES(
                      N'RolePermission',N'BULK_UPDATE',CONCAT(N'{"RoleId":',@RoleId,N'}'),
                      N'["PermissionPolicyIds"]',@Old,@New,@ActorUserId,@ActorName,
                      @TraceId,@IpAddress,@Now);
                  END;

                  COMMIT;
                  SELECT COUNT(*)
                  FROM dbo.RolePermission
                  WHERE RoleId=@RoleId AND IsActive=1;
                END;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentsBulkSet;");
            migrationBuilder.Sql(
                """
                DELETE assignment
                FROM dbo.RolePermission assignment
                JOIN dbo.PermissionPolicy policy ON policy.Id=assignment.PermissionPolicyId
                WHERE policy.Code=N'PermissionAssignment.Manage';
                DELETE FROM dbo.PermissionPolicy
                WHERE Code=N'PermissionAssignment.Manage';
                """);

        }
    }
}
