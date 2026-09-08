using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPortalPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresMenuAccess",
                schema: "dbo",
                table: "PermissionPolicy",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(
                """
                MERGE dbo.PermissionPolicy AS target
                USING (VALUES
                  (N'Organization.View',N'View organization',N'View organization details.',N'ORGANIZATION',N'Organization',N'Index',1),
                  (N'Organization.Edit',N'Edit organization',N'Edit organization details.',N'ORGANIZATION',N'Organization',N'Index',1),
                  (N'OfficeBranch.View',N'View office branches',N'View and search office branches.',N'ORGANIZATION',N'Organization',N'OfficeBranches',1),
                  (N'OfficeBranch.Create',N'Create office branches',N'Create office branches.',N'ORGANIZATION',N'Organization',N'OfficeBranches',1),
                  (N'OfficeBranch.Edit',N'Edit office branches',N'Edit office branches.',N'ORGANIZATION',N'Organization',N'OfficeBranches',1),
                  (N'OfficeBranch.Delete',N'Delete office branches',N'Deactivate office branches.',N'ORGANIZATION',N'Organization',N'OfficeBranches',1),
                  (N'Department.View',N'View departments',N'View and search departments.',N'ORGANIZATION',N'Organization',N'Departments',1),
                  (N'Department.Create',N'Create departments',N'Create departments.',N'ORGANIZATION',N'Organization',N'Departments',1),
                  (N'Department.Edit',N'Edit departments',N'Edit departments.',N'ORGANIZATION',N'Organization',N'Departments',1),
                  (N'Department.Delete',N'Delete departments',N'Deactivate departments.',N'ORGANIZATION',N'Organization',N'Departments',1),
                  (N'FiscalYear.View',N'View fiscal years',N'View and search fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1),
                  (N'FiscalYear.Create',N'Create fiscal years',N'Create draft or active fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1),
                  (N'FiscalYear.Edit',N'Edit fiscal years',N'Edit non-closed fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1),
                  (N'FiscalYear.Close',N'Close fiscal years',N'Permanently close fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1),
                  (N'FiscalYear.Delete',N'Delete fiscal years',N'Delete draft fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1),
                  (N'ListItemCategory.View',N'View list item categories',N'View list item categories.',N'ORGANIZATION',N'ListItem',N'Index',1),
                  (N'ListItemCategory.Create',N'Create list item categories',N'Create list item categories.',N'ORGANIZATION',N'ListItem',N'Index',1),
                  (N'ListItemCategory.Edit',N'Edit list item categories',N'Edit list item categories.',N'ORGANIZATION',N'ListItem',N'Index',1),
                  (N'ListItemCategory.Delete',N'Delete list item categories',N'Deactivate list item categories.',N'ORGANIZATION',N'ListItem',N'Index',1),
                  (N'ListItem.View',N'View list items',N'View list items.',N'ORGANIZATION',N'ListItem',N'ListItem',1),
                  (N'ListItem.Create',N'Create list items',N'Create list items.',N'ORGANIZATION',N'ListItem',N'ListItem',1),
                  (N'ListItem.Edit',N'Edit list items',N'Edit list items.',N'ORGANIZATION',N'ListItem',N'ListItem',1),
                  (N'ListItem.Delete',N'Delete list items',N'Deactivate list items.',N'ORGANIZATION',N'ListItem',N'ListItem',1),
                  (N'Role.View',N'View roles',N'View roles and module options.',N'ORGANIZATION',N'Setup',N'Role',1),
                  (N'Role.Create',N'Create roles',N'Create roles and initial module assignments.',N'ORGANIZATION',N'Setup',N'Role',1),
                  (N'Role.Edit',N'Edit roles',N'Edit roles and module assignments.',N'ORGANIZATION',N'Setup',N'Role',1),
                  (N'Role.Delete',N'Delete roles',N'Deactivate roles.',N'ORGANIZATION',N'Setup',N'Role',1),
                  (N'Role.AssignUsers',N'Assign users to roles',N'Assign or remove individual role memberships.',N'ORGANIZATION',N'Setup',N'Role',1),
                  (N'Module.View',N'View modules',N'View modules, menus, and configuration.',N'ORGANIZATION',N'Setup',N'Module',1),
                  (N'Module.Create',N'Create modules',N'Create application modules and menus.',N'ORGANIZATION',N'Setup',N'Module',1),
                  (N'Module.Edit',N'Edit modules',N'Edit application modules and menus.',N'ORGANIZATION',N'Setup',N'Module',1),
                  (N'Module.Delete',N'Delete modules',N'Deactivate application modules.',N'ORGANIZATION',N'Setup',N'Module',1),
                  (N'Module.Configure',N'Configure modules',N'Configure module roles and menu assignments.',N'ORGANIZATION',N'Setup',N'Module',1),
                  (N'RoleModuleMenu.View',N'View role menu assignments',N'View role module-menu assignments.',N'ORGANIZATION',N'RoleModuleMenu',N'Index',1),
                  (N'RoleModuleMenu.Create',N'Create role menu assignments',N'Assign menus to roles.',N'ORGANIZATION',N'RoleModuleMenu',N'Index',1),
                  (N'RoleModuleMenu.Edit',N'Edit role menu assignments',N'Edit role menu assignments.',N'ORGANIZATION',N'RoleModuleMenu',N'Index',1),
                  (N'RoleModuleMenu.Delete',N'Delete role menu assignments',N'Deactivate role menu assignments.',N'ORGANIZATION',N'RoleModuleMenu',N'Index',1),
                  (N'User.View',N'View users',N'View users and organization options.',N'ORGANIZATION',N'User',N'Index',1),
                  (N'User.Create',N'Create users',N'Create users.',N'ORGANIZATION',N'User',N'Index',1),
                  (N'User.Edit',N'Edit users',N'Edit users.',N'ORGANIZATION',N'User',N'Index',1),
                  (N'User.Delete',N'Delete users',N'Deactivate users.',N'ORGANIZATION',N'User',N'Index',1),
                  (N'User.AssignRoles',N'Assign user roles',N'Assign roles to users.',N'ORGANIZATION',N'User',N'Index',1),
                  (N'Workflow.View',N'View workflows',N'View workflow configuration.',N'ORGANIZATION',N'Workflow',N'Index',1),
                  (N'Workflow.Create',N'Create workflows',N'Create workflow configuration.',N'ORGANIZATION',N'Workflow',N'Index',1),
                  (N'Workflow.Edit',N'Edit workflows',N'Edit workflow configuration.',N'ORGANIZATION',N'Workflow',N'Index',1),
                  (N'Workflow.Delete',N'Delete workflows',N'Deactivate workflow configuration.',N'ORGANIZATION',N'Workflow',N'Index',1),
                  (N'AuditLog.View',N'View audit logs',N'View and filter audit logs.',N'ORGANIZATION',N'AuditLog',N'Index',1),
                  (N'AuditLog.Export',N'Export audit logs',N'Export filtered audit logs.',N'ORGANIZATION',N'AuditLog',N'Index',1),
                  (N'PermissionAssignment.View',N'View permission assignments',N'View role permission assignments.',N'ORGANIZATION',N'Setup',N'Permission',1),
                  (N'PermissionAssignment.Create',N'Create permission assignments',N'Grant permissions to roles.',N'ORGANIZATION',N'Setup',N'Permission',1),
                  (N'PermissionAssignment.Edit',N'Edit permission assignments',N'Activate or deactivate role permissions.',N'ORGANIZATION',N'Setup',N'Permission',1),
                  (N'PermissionAssignment.Delete',N'Delete permission assignments',N'Revoke permissions from roles.',N'ORGANIZATION',N'Setup',N'Permission',1),
                  (N'Portal.View',N'Open portal',N'Open the application portal.',N'PORTAL',N'Portal',N'Index',0),
                  (N'Portal.SelectModule',N'Select portal modules',N'Open a module from the portal.',N'PORTAL',N'Portal',N'Module',0)
                ) AS source(Code,Name,Description,ModuleCode,MenuController,MenuAction,RequiresMenuAccess)
                ON target.Code=source.Code
                WHEN MATCHED THEN UPDATE SET Name=source.Name,Description=source.Description,
                  ModuleCode=source.ModuleCode,MenuController=source.MenuController,MenuAction=source.MenuAction,
                  RequiresMenuAccess=source.RequiresMenuAccess,IsActive=1,UpdatedAtUtc=SYSUTCDATETIME()
                WHEN NOT MATCHED THEN INSERT(Code,Name,Description,ModuleCode,MenuController,MenuAction,
                  RequiresMenuAccess,IsActive,CreatedAtUtc)
                  VALUES(source.Code,source.Name,source.Description,source.ModuleCode,source.MenuController,
                    source.MenuAction,source.RequiresMenuAccess,1,SYSUTCDATETIME());

                UPDATE rp SET IsActive=1,ModifiedAtUtc=SYSUTCDATETIME(),ModifiedBy=N'System permission catalog migration'
                FROM dbo.RolePermission rp
                JOIN dbo.Role r ON r.Id=rp.RoleId
                JOIN dbo.PermissionPolicy p ON p.Id=rp.PermissionPolicyId
                WHERE r.NormalizedName=N'ADMIN' AND r.IsActive=1 AND p.IsActive=1;

                INSERT dbo.RolePermission(RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy)
                SELECT r.Id,p.Id,1,SYSUTCDATETIME(),N'System permission catalog migration'
                FROM dbo.Role r CROSS JOIN dbo.PermissionPolicy p
                WHERE r.NormalizedName=N'ADMIN' AND r.IsActive=1 AND p.IsActive=1
                  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermission rp
                    WHERE rp.RoleId=r.Id AND rp.PermissionPolicyId=p.Id);
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionRoleDdl AS
                BEGIN
                  SET NOCOUNT ON;
                  SELECT Id,Name FROM dbo.Role
                  WHERE IsActive=1 AND NormalizedName<>N'SUPERADMIN'
                  ORDER BY Name;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionPolicyDdl @RoleId int AS
                BEGIN SET NOCOUNT ON;
                  SELECT p.Id,p.Code,p.Name,p.ModuleCode,p.MenuController+N' / '+p.MenuAction MenuName,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId=@RoleId AND rp.PermissionPolicyId=p.Id AND rp.IsActive=1) THEN 1 ELSE 0 END) IsAssigned,
                    CONVERT(bit,CASE WHEN p.RequiresMenuAccess=0 OR EXISTS(SELECT 1 FROM dbo.ApplicationModule am JOIN dbo.RoleModule rm ON rm.ApplicationModuleId=am.Id AND rm.RoleId=@RoleId AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE am.Code=p.ModuleCode AND am.IsActive=1) THEN 1 ELSE 0 END) CanAssign
                  FROM dbo.PermissionPolicy p WHERE p.IsActive=1 ORDER BY p.ModuleCode,p.MenuController,p.MenuAction,p.Name;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentIns @RoleId int,@PermissionPolicyId int,@IsActive bit=1,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL AS
                BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
                  IF NOT EXISTS(SELECT 1 FROM dbo.Role WHERE Id=@RoleId AND IsActive=1) THROW 50310,'Active role was not found.',1;
                  IF EXISTS(SELECT 1 FROM dbo.Role WHERE Id=@RoleId AND NormalizedName=N'SUPERADMIN') THROW 50315,'SuperAdmin has global access and does not accept permission assignments.',1;
                  IF NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy WHERE Id=@PermissionPolicyId AND IsActive=1) THROW 50311,'Active permission policy was not found.',1;
                  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy p WHERE p.Id=@PermissionPolicyId AND (p.RequiresMenuAccess=0 OR EXISTS(SELECT 1 FROM dbo.ApplicationModule am JOIN dbo.RoleModule rm ON rm.ApplicationModuleId=am.Id AND rm.RoleId=@RoleId AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE am.Code=p.ModuleCode AND am.IsActive=1))) THROW 50312,'Assign the policy menu to the role before granting this permission.',1;
                  IF EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId) THROW 50313,'This permission assignment already exists.',1;
                  BEGIN TRANSACTION;DECLARE @Now datetime2(0)=SYSUTCDATETIME();INSERT dbo.RolePermission(RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedByUserId,AssignedBy) VALUES(@RoleId,@PermissionPolicyId,@IsActive,@Now,@ActorUserId,@ActorName);
                  DECLARE @New nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc) VALUES(N'RolePermission',N'INSERT',CONCAT(N'{"RoleId":',@RoleId,N',"PermissionPolicyId":',@PermissionPolicyId,N'}'),N'["RoleId","PermissionPolicyId","IsActive"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                  COMMIT;EXEC dbo.SPPermissionAssignmentGetById @RoleId,@PermissionPolicyId;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentUpd @OriginalRoleId int,@OriginalPermissionPolicyId int,@RoleId int,@PermissionPolicyId int,@IsActive bit,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL AS
                BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
                  IF @OriginalRoleId<>@RoleId OR @OriginalPermissionPolicyId<>@PermissionPolicyId THROW 50314,'Role and permission cannot be changed; create another assignment.',1;
                  IF NOT EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId) RETURN;
                  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy p WHERE p.Id=@PermissionPolicyId AND (p.RequiresMenuAccess=0 OR EXISTS(SELECT 1 FROM dbo.ApplicationModule am JOIN dbo.RoleModule rm ON rm.ApplicationModuleId=am.Id AND rm.RoleId=@RoleId AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE am.Code=p.ModuleCode AND am.IsActive=1))) THROW 50312,'Assign the policy menu to the role before granting this permission.',1;
                  BEGIN TRANSACTION;DECLARE @Old nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now datetime2(0)=SYSUTCDATETIME();
                  UPDATE dbo.RolePermission SET IsActive=@IsActive,ModifiedAtUtc=@Now,ModifiedByUserId=@ActorUserId,ModifiedBy=@ActorName WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId;
                  DECLARE @New nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc) VALUES(N'RolePermission',N'UPDATE',CONCAT(N'{"RoleId":',@RoleId,N',"PermissionPolicyId":',@PermissionPolicyId,N'}'),N'["IsActive"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                  COMMIT;EXEC dbo.SPPermissionAssignmentGetById @RoleId,@PermissionPolicyId;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionRoleDdl AS
                BEGIN SET NOCOUNT ON;SELECT Id,Name FROM dbo.Role WHERE IsActive=1 ORDER BY Name;END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionPolicyDdl @RoleId int AS
                BEGIN SET NOCOUNT ON;
                  SELECT p.Id,p.Code,p.Name,p.ModuleCode,p.MenuController+N' / '+p.MenuAction MenuName,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId=@RoleId AND rp.PermissionPolicyId=p.Id AND rp.IsActive=1) THEN 1 ELSE 0 END) IsAssigned,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.ApplicationModule am JOIN dbo.RoleModule rm ON rm.ApplicationModuleId=am.Id AND rm.RoleId=@RoleId AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE am.Code=p.ModuleCode AND am.IsActive=1) THEN 1 ELSE 0 END) CanAssign
                  FROM dbo.PermissionPolicy p WHERE p.IsActive=1 ORDER BY p.ModuleCode,p.MenuController,p.MenuAction,p.Name;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentIns @RoleId int,@PermissionPolicyId int,@IsActive bit=1,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL AS
                BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
                  IF NOT EXISTS(SELECT 1 FROM dbo.Role WHERE Id=@RoleId AND IsActive=1) THROW 50310,'Active role was not found.',1;
                  IF NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy WHERE Id=@PermissionPolicyId AND IsActive=1) THROW 50311,'Active permission policy was not found.',1;
                  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy p JOIN dbo.ApplicationModule am ON am.Code=p.ModuleCode AND am.IsActive=1 JOIN dbo.RoleModule rm ON rm.RoleId=@RoleId AND rm.ApplicationModuleId=am.Id AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE p.Id=@PermissionPolicyId) THROW 50312,'Assign the policy menu to the role before granting this permission.',1;
                  IF EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId) THROW 50313,'This permission assignment already exists.',1;
                  BEGIN TRANSACTION;DECLARE @Now datetime2(0)=SYSUTCDATETIME();INSERT dbo.RolePermission(RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedByUserId,AssignedBy) VALUES(@RoleId,@PermissionPolicyId,@IsActive,@Now,@ActorUserId,@ActorName);
                  DECLARE @New nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc) VALUES(N'RolePermission',N'INSERT',CONCAT(N'{"RoleId":',@RoleId,N',"PermissionPolicyId":',@PermissionPolicyId,N'}'),N'["RoleId","PermissionPolicyId","IsActive"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                  COMMIT;EXEC dbo.SPPermissionAssignmentGetById @RoleId,@PermissionPolicyId;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentUpd @OriginalRoleId int,@OriginalPermissionPolicyId int,@RoleId int,@PermissionPolicyId int,@IsActive bit,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL AS
                BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
                  IF @OriginalRoleId<>@RoleId OR @OriginalPermissionPolicyId<>@PermissionPolicyId THROW 50314,'Role and permission cannot be changed; create another assignment.',1;
                  IF NOT EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId) RETURN;
                  IF @IsActive=1 AND NOT EXISTS(SELECT 1 FROM dbo.PermissionPolicy p JOIN dbo.ApplicationModule am ON am.Code=p.ModuleCode AND am.IsActive=1 JOIN dbo.RoleModule rm ON rm.RoleId=@RoleId AND rm.ApplicationModuleId=am.Id AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE p.Id=@PermissionPolicyId) THROW 50312,'Assign the policy menu to the role before granting this permission.',1;
                  BEGIN TRANSACTION;DECLARE @Old nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now datetime2(0)=SYSUTCDATETIME();
                  UPDATE dbo.RolePermission SET IsActive=@IsActive,ModifiedAtUtc=@Now,ModifiedByUserId=@ActorUserId,ModifiedBy=@ActorName WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId;
                  DECLARE @New nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc) VALUES(N'RolePermission',N'UPDATE',CONCAT(N'{"RoleId":',@RoleId,N',"PermissionPolicyId":',@PermissionPolicyId,N'}'),N'["IsActive"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                  COMMIT;EXEC dbo.SPPermissionAssignmentGetById @RoleId,@PermissionPolicyId;
                END;
                """);

            migrationBuilder.Sql(
                """
                DELETE rp FROM dbo.RolePermission rp JOIN dbo.PermissionPolicy p ON p.Id=rp.PermissionPolicyId
                WHERE p.Code IN (
                  N'Organization.View',N'Organization.Edit',N'OfficeBranch.View',N'OfficeBranch.Create',N'OfficeBranch.Edit',N'OfficeBranch.Delete',
                  N'Department.View',N'Department.Create',N'Department.Edit',N'Department.Delete',N'ListItemCategory.View',N'ListItemCategory.Create',
                  N'ListItemCategory.Edit',N'ListItemCategory.Delete',N'ListItem.View',N'ListItem.Create',N'ListItem.Edit',N'ListItem.Delete',
                  N'Role.View',N'Role.Create',N'Role.Edit',N'Role.Delete',N'Role.AssignUsers',N'Module.View',N'Module.Create',N'Module.Edit',
                  N'Module.Delete',N'Module.Configure',N'RoleModuleMenu.View',N'RoleModuleMenu.Create',N'RoleModuleMenu.Edit',N'RoleModuleMenu.Delete',
                  N'User.View',N'User.Create',N'User.Edit',N'User.Delete',N'User.AssignRoles',N'Workflow.View',N'Workflow.Create',N'Workflow.Edit',
                  N'Workflow.Delete',N'AuditLog.View',N'AuditLog.Export',N'PermissionAssignment.View',N'PermissionAssignment.Create',
                  N'PermissionAssignment.Edit',N'PermissionAssignment.Delete',N'Portal.View',N'Portal.SelectModule');
                DELETE FROM dbo.PermissionPolicy WHERE Code IN (
                  N'Organization.View',N'Organization.Edit',N'OfficeBranch.View',N'OfficeBranch.Create',N'OfficeBranch.Edit',N'OfficeBranch.Delete',
                  N'Department.View',N'Department.Create',N'Department.Edit',N'Department.Delete',N'ListItemCategory.View',N'ListItemCategory.Create',
                  N'ListItemCategory.Edit',N'ListItemCategory.Delete',N'ListItem.View',N'ListItem.Create',N'ListItem.Edit',N'ListItem.Delete',
                  N'Role.View',N'Role.Create',N'Role.Edit',N'Role.Delete',N'Role.AssignUsers',N'Module.View',N'Module.Create',N'Module.Edit',
                  N'Module.Delete',N'Module.Configure',N'RoleModuleMenu.View',N'RoleModuleMenu.Create',N'RoleModuleMenu.Edit',N'RoleModuleMenu.Delete',
                  N'User.View',N'User.Create',N'User.Edit',N'User.Delete',N'User.AssignRoles',N'Workflow.View',N'Workflow.Create',N'Workflow.Edit',
                  N'Workflow.Delete',N'AuditLog.View',N'AuditLog.Export',N'PermissionAssignment.View',N'PermissionAssignment.Create',
                  N'PermissionAssignment.Edit',N'PermissionAssignment.Delete',N'Portal.View',N'Portal.SelectModule');
                """);

            migrationBuilder.DropColumn(
                name: "RequiresMenuAccess",
                schema: "dbo",
                table: "PermissionPolicy");
        }
    }
}
