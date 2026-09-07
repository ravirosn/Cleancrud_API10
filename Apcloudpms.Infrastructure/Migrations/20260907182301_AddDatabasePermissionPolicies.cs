using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabasePermissionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionPolicy",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModuleCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MenuController = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MenuAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionPolicyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.RoleId, x.PermissionPolicyId });
                    table.ForeignKey(
                        name: "FK_RolePermission_PermissionPolicy_PermissionPolicyId",
                        column: x => x.PermissionPolicyId,
                        principalSchema: "dbo",
                        principalTable: "PermissionPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionPolicy_Code",
                schema: "dbo",
                table: "PermissionPolicy",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionPolicy_ModuleCode_MenuController_MenuAction_IsActive",
                schema: "dbo",
                table: "PermissionPolicy",
                columns: new[] { "ModuleCode", "MenuController", "MenuAction", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionPolicyId_IsActive",
                schema: "dbo",
                table: "RolePermission",
                columns: new[] { "PermissionPolicyId", "IsActive" });

            migrationBuilder.Sql("""
                INSERT dbo.PermissionPolicy(Code,Name,Description,ModuleCode,MenuController,MenuAction,IsActive,CreatedAtUtc)
                VALUES
                (N'FiscalYear.View',N'View fiscal years',N'View and search fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1,SYSUTCDATETIME()),
                (N'FiscalYear.Create',N'Create fiscal years',N'Create draft or active fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1,SYSUTCDATETIME()),
                (N'FiscalYear.Edit',N'Edit fiscal years',N'Edit non-closed fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1,SYSUTCDATETIME()),
                (N'FiscalYear.Close',N'Close fiscal years',N'Permanently close fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1,SYSUTCDATETIME()),
                (N'FiscalYear.Delete',N'Delete fiscal years',N'Delete draft fiscal years.',N'ORGANIZATION',N'Organization',N'FiscalYears',1,SYSUTCDATETIME());

                INSERT dbo.RolePermission(RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy)
                SELECT r.Id,p.Id,1,SYSUTCDATETIME(),N'System migration'
                FROM dbo.Role r CROSS JOIN dbo.PermissionPolicy p
                WHERE r.NormalizedName=N'ADMIN' AND r.IsActive=1 AND p.Code LIKE N'FiscalYear.%';
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentGet
                  @PageNumber int=1,@PageSize int=10,@SearchTerm nvarchar(200)=NULL,@SortBy nvarchar(30)=N'assignedAtUtc',@SortDirection varchar(4)='desc',@IncludeInactive bit=0
                AS
                BEGIN
                  SET NOCOUNT ON;
                  IF @PageNumber<1 THROW 50300,'PageNumber must be greater than zero.',1;
                  IF @PageSize<1 OR @PageSize>100 THROW 50301,'PageSize must be between 1 and 100.',1;
                  SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'');SET @SortBy=LOWER(@SortBy);SET @SortDirection=LOWER(@SortDirection);
                  IF @SortBy NOT IN(N'rolename',N'permissioncode',N'permissionname',N'modulecode',N'menuname',N'status',N'assignedatutc') SET @SortBy=N'assignedatutc';
                  IF @SortDirection NOT IN('asc','desc') SET @SortDirection='desc';
                  DECLARE @Pattern nvarchar(402)=CASE WHEN @SearchTerm IS NULL THEN NULL ELSE N'%'+REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[')+N'%' END;
                  SELECT COUNT_BIG(1) TotalRecords FROM dbo.RolePermission rp JOIN dbo.Role r ON r.Id=rp.RoleId JOIN dbo.PermissionPolicy p ON p.Id=rp.PermissionPolicyId
                  WHERE (@IncludeInactive=1 OR rp.IsActive=1) AND (@Pattern IS NULL OR r.Name LIKE @Pattern ESCAPE N'\' OR p.Code LIKE @Pattern ESCAPE N'\' OR p.Name LIKE @Pattern ESCAPE N'\' OR p.ModuleCode LIKE @Pattern ESCAPE N'\');
                  SELECT rp.RoleId,r.Name RoleName,rp.PermissionPolicyId,p.Code PermissionCode,p.Name PermissionName,p.ModuleCode,p.MenuController,p.MenuAction,rp.IsActive,rp.AssignedAtUtc,rp.AssignedBy,rp.ModifiedAtUtc,rp.ModifiedBy
                  FROM dbo.RolePermission rp JOIN dbo.Role r ON r.Id=rp.RoleId JOIN dbo.PermissionPolicy p ON p.Id=rp.PermissionPolicyId
                  WHERE (@IncludeInactive=1 OR rp.IsActive=1) AND (@Pattern IS NULL OR r.Name LIKE @Pattern ESCAPE N'\' OR p.Code LIKE @Pattern ESCAPE N'\' OR p.Name LIKE @Pattern ESCAPE N'\' OR p.ModuleCode LIKE @Pattern ESCAPE N'\')
                  ORDER BY
                    CASE WHEN @SortBy=N'rolename' AND @SortDirection='asc' THEN r.Name END ASC,CASE WHEN @SortBy=N'rolename' AND @SortDirection='desc' THEN r.Name END DESC,
                    CASE WHEN @SortBy=N'permissioncode' AND @SortDirection='asc' THEN p.Code END ASC,CASE WHEN @SortBy=N'permissioncode' AND @SortDirection='desc' THEN p.Code END DESC,
                    CASE WHEN @SortBy=N'permissionname' AND @SortDirection='asc' THEN p.Name END ASC,CASE WHEN @SortBy=N'permissionname' AND @SortDirection='desc' THEN p.Name END DESC,
                    CASE WHEN @SortBy=N'modulecode' AND @SortDirection='asc' THEN p.ModuleCode END ASC,CASE WHEN @SortBy=N'modulecode' AND @SortDirection='desc' THEN p.ModuleCode END DESC,
                    CASE WHEN @SortBy=N'menuname' AND @SortDirection='asc' THEN p.MenuController+N'.'+p.MenuAction END ASC,CASE WHEN @SortBy=N'menuname' AND @SortDirection='desc' THEN p.MenuController+N'.'+p.MenuAction END DESC,
                    CASE WHEN @SortBy=N'status' AND @SortDirection='asc' THEN rp.IsActive END ASC,CASE WHEN @SortBy=N'status' AND @SortDirection='desc' THEN rp.IsActive END DESC,
                    CASE WHEN @SortBy=N'assignedatutc' AND @SortDirection='asc' THEN rp.AssignedAtUtc END ASC,CASE WHEN @SortBy=N'assignedatutc' AND @SortDirection='desc' THEN rp.AssignedAtUtc END DESC,rp.RoleId,p.Id
                  OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPPermissionRoleDdl AS
                BEGIN SET NOCOUNT ON;SELECT Id,Name FROM dbo.Role WHERE IsActive=1 ORDER BY Name;END;
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPPermissionPolicyDdl @RoleId int AS
                BEGIN SET NOCOUNT ON;
                  SELECT p.Id,p.Code,p.Name,p.ModuleCode,p.MenuController+N' / '+p.MenuAction MenuName,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId=@RoleId AND rp.PermissionPolicyId=p.Id AND rp.IsActive=1) THEN 1 ELSE 0 END) IsAssigned,
                    CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.ApplicationModule am JOIN dbo.RoleModule rm ON rm.ApplicationModuleId=am.Id AND rm.RoleId=@RoleId AND rm.IsActive=1 JOIN dbo.ModuleMenu mm ON mm.ApplicationModuleId=am.Id AND mm.ControllerName=p.MenuController AND mm.ActionName=p.MenuAction AND mm.IsActive=1 JOIN dbo.RoleModuleMenu rmm ON rmm.RoleId=@RoleId AND rmm.ApplicationModuleId=am.Id AND rmm.ModuleMenuId=mm.Id AND rmm.IsActive=1 WHERE am.Code=p.ModuleCode AND am.IsActive=1) THEN 1 ELSE 0 END) CanAssign
                  FROM dbo.PermissionPolicy p WHERE p.IsActive=1 ORDER BY p.ModuleCode,p.MenuController,p.MenuAction,p.Name;
                END;
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentGetById @RoleId int,@PermissionPolicyId int AS
                BEGIN SET NOCOUNT ON;
                  SELECT rp.RoleId,r.Name RoleName,rp.PermissionPolicyId,p.Code PermissionCode,p.Name PermissionName,p.ModuleCode,p.MenuController,p.MenuAction,rp.IsActive,rp.AssignedAtUtc,rp.AssignedBy,rp.ModifiedAtUtc,rp.ModifiedBy
                  FROM dbo.RolePermission rp JOIN dbo.Role r ON r.Id=rp.RoleId JOIN dbo.PermissionPolicy p ON p.Id=rp.PermissionPolicyId
                  WHERE rp.RoleId=@RoleId AND rp.PermissionPolicyId=@PermissionPolicyId;
                END;
                """);

            migrationBuilder.Sql("""
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

            migrationBuilder.Sql("""
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

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.SPPermissionAssignmentDel @RoleId int,@PermissionPolicyId int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL AS
                BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
                  IF NOT EXISTS(SELECT 1 FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId) BEGIN SELECT 0;RETURN;END;
                  BEGIN TRANSACTION;DECLARE @Old nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now datetime2(0)=SYSUTCDATETIME();
                  UPDATE dbo.RolePermission SET IsActive=0,ModifiedAtUtc=@Now,ModifiedByUserId=@ActorUserId,ModifiedBy=@ActorName WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId;
                  DECLARE @New nvarchar(max)=(SELECT RoleId,PermissionPolicyId,IsActive FROM dbo.RolePermission WHERE RoleId=@RoleId AND PermissionPolicyId=@PermissionPolicyId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
                  INSERT dbo.AuditLog(EntityName,Action,EntityKey,ChangedColumns,OldValues,NewValues,ChangedByUserId,ChangedBy,TraceId,IpAddress,ChangedAtUtc) VALUES(N'RolePermission',N'DEACTIVATE',CONCAT(N'{"RoleId":',@RoleId,N',"PermissionPolicyId":',@PermissionPolicyId,N'}'),N'["IsActive"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);COMMIT;SELECT 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentDel;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentUpd;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentIns;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentGetById;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionPolicyDdl;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionRoleDdl;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.SPPermissionAssignmentGet;");
            migrationBuilder.DropTable(
                name: "RolePermission",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PermissionPolicy",
                schema: "dbo");
        }
    }
}
