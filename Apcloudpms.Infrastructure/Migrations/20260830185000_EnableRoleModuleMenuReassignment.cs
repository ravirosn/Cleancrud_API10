using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260830185000_EnableRoleModuleMenuReassignment")]
public sealed class EnableRoleModuleMenuReassignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(SqlBatch(UpdatedProcedure));

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(SqlBatch(PreviousProcedure));

    private const string UpdatedProcedure = """
        CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusEdit]
            @OriginalRoleId int,@OriginalApplicationModuleId int,@OriginalModuleMenuId int,
            @RoleId int,@ApplicationModuleId int,@ModuleMenuId int,@IsActive bit,
            @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@ParentMenuId int,
                @KeysChanged bit=CASE WHEN @OriginalRoleId<>@RoleId
                    OR @OriginalApplicationModuleId<>@ApplicationModuleId
                    OR @OriginalModuleMenuId<>@ModuleMenuId THEN 1 ELSE 0 END;
            SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''),N'System');
            BEGIN TRANSACTION;

            IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                WHERE [RoleId]=@OriginalRoleId AND [ApplicationModuleId]=@OriginalApplicationModuleId
                  AND [ModuleMenuId]=@OriginalModuleMenuId)
                THROW 50004,'The original role menu assignment was not found.',1;

            IF @KeysChanged=1 OR @IsActive=1
            BEGIN
                IF NOT EXISTS(SELECT 1 FROM [dbo].[Role] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [Id]=@RoleId AND [IsActive]=1)
                    THROW 50061,'The selected role does not exist or is inactive.',1;
                IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModule] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [IsActive]=1)
                    THROW 50060,'Assign the selected module to this role before assigning its menus.',1;
                SELECT @ParentMenuId=[ParentMenuId] FROM [dbo].[ModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [Id]=@ModuleMenuId AND [ApplicationModuleId]=@ApplicationModuleId AND [IsActive]=1;
                IF @@ROWCOUNT=0
                    THROW 50062,'The selected menu does not belong to this module or is inactive.',1;
                IF @IsActive=1 AND @ParentMenuId IS NOT NULL AND NOT EXISTS(
                    SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                      AND [ModuleMenuId]=@ParentMenuId AND [IsActive]=1)
                    THROW 50063,'Assign the parent menu before assigning this child menu.',1;
            END

            IF (@KeysChanged=1 OR @IsActive=0) AND EXISTS(
                SELECT 1 FROM [dbo].[RoleModuleMenu] childAssignment WITH(UPDLOCK,HOLDLOCK)
                INNER JOIN [dbo].[ModuleMenu] childMenu
                    ON childMenu.[Id]=childAssignment.[ModuleMenuId]
                   AND childMenu.[ApplicationModuleId]=childAssignment.[ApplicationModuleId]
                WHERE childAssignment.[RoleId]=@OriginalRoleId
                  AND childAssignment.[ApplicationModuleId]=@OriginalApplicationModuleId
                  AND childMenu.[ParentMenuId]=@OriginalModuleMenuId
                  AND childAssignment.[IsActive]=1)
                THROW 50066,'Deactivate the assigned child menus before changing their parent menu assignment.',1;

            DECLARE @Old nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],
                    [IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
                FROM [dbo].[RoleModuleMenu]
                WHERE [RoleId]=@OriginalRoleId AND [ApplicationModuleId]=@OriginalApplicationModuleId
                  AND [ModuleMenuId]=@OriginalModuleMenuId
                FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);

            IF @KeysChanged=1
            BEGIN
                IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                      AND [ModuleMenuId]=@ModuleMenuId AND [IsActive]=1)
                    THROW 50067,'The selected role already has an active assignment to this menu.',1;

                UPDATE [dbo].[RoleModuleMenu]
                SET [IsActive]=0,[ModifiedAtUtc]=@Now,[ModifiedBy]=@ActorName
                WHERE [RoleId]=@OriginalRoleId AND [ApplicationModuleId]=@OriginalApplicationModuleId
                  AND [ModuleMenuId]=@OriginalModuleMenuId;

                IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                      AND [ModuleMenuId]=@ModuleMenuId)
                    UPDATE [dbo].[RoleModuleMenu]
                    SET [IsActive]=@IsActive,[AssignedAtUtc]=@Now,[AssignedBy]=@ActorName,
                        [ModifiedAtUtc]=NULL,[ModifiedBy]=NULL
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                      AND [ModuleMenuId]=@ModuleMenuId;
                ELSE
                    INSERT [dbo].[RoleModuleMenu]
                        ([RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy])
                    VALUES(@RoleId,@ApplicationModuleId,@ModuleMenuId,@IsActive,@Now,@ActorName);
            END
            ELSE
                UPDATE [dbo].[RoleModuleMenu]
                SET [IsActive]=@IsActive,[ModifiedAtUtc]=@Now,[ModifiedBy]=@ActorName
                WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                  AND [ModuleMenuId]=@ModuleMenuId;

            DECLARE @New nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],
                    [IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
                FROM [dbo].[RoleModuleMenu]
                WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                  AND [ModuleMenuId]=@ModuleMenuId
                FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog]
                ([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],
                 [ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
            VALUES(N'RoleModuleMenu',CASE WHEN @KeysChanged=1 THEN N'Reassigned' ELSE N'Modified' END,
                CONCAT(N'{"RoleId":',@RoleId,N',"ApplicationModuleId":',@ApplicationModuleId,
                    N',"ModuleMenuId":',@ModuleMenuId,N'}'),
                CASE WHEN @KeysChanged=1
                    THEN N'["RoleId","ApplicationModuleId","ModuleMenuId","IsActive","AssignedAtUtc","AssignedBy"]'
                    ELSE N'["IsActive","ModifiedAtUtc","ModifiedBy"]' END,
                @Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;

            ;WITH MenuTree AS
            (
                SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],
                    CAST(m.[Name] AS nvarchar(2000)) [Hierarchy]
                FROM [dbo].[ModuleMenu] m
                WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[ParentMenuId] IS NULL
                UNION ALL
                SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],
                    CAST(CONCAT(t.[Hierarchy],N' / ',m.[Name]) AS nvarchar(2000))
                FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t
                    ON t.[Id]=m.[ParentMenuId] AND t.[ApplicationModuleId]=m.[ApplicationModuleId]
            )
            SELECT x.[RoleId],r.[Name] [RoleName],x.[ApplicationModuleId],am.[Name] [ModuleName],
                x.[ModuleMenuId],t.[ParentMenuId],t.[Name] [MenuName],t.[Hierarchy] [MenuHierarchy],
                t.[DisplayOrder],x.[IsActive],x.[AssignedAtUtc],x.[AssignedBy],x.[ModifiedAtUtc],x.[ModifiedBy]
            FROM [dbo].[RoleModuleMenu] x
            INNER JOIN [dbo].[Role] r ON r.[Id]=x.[RoleId]
            INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=x.[ApplicationModuleId]
            INNER JOIN MenuTree t ON t.[Id]=x.[ModuleMenuId]
                AND t.[ApplicationModuleId]=x.[ApplicationModuleId]
            WHERE x.[RoleId]=@RoleId AND x.[ApplicationModuleId]=@ApplicationModuleId
              AND x.[ModuleMenuId]=@ModuleMenuId
            OPTION(MAXRECURSION 100);
        END;
        """;

    private const string PreviousProcedure = """
        CREATE OR ALTER PROCEDURE [dbo].[SpRoleModuleMenusEdit]
            @RoleId int,@ApplicationModuleId int,@ModuleMenuId int,@IsActive bit,
            @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@ParentMenuId int;
            SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''),N'System');
            BEGIN TRANSACTION;
            IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId)
                THROW 50004,'The role menu assignment was not found.',1;
            SELECT @ParentMenuId=[ParentMenuId] FROM [dbo].[ModuleMenu]
                WHERE [Id]=@ModuleMenuId AND [ApplicationModuleId]=@ApplicationModuleId;
            IF @IsActive=1
            BEGIN
                IF NOT EXISTS(SELECT 1 FROM [dbo].[RoleModule] rm INNER JOIN [dbo].[Role] r ON r.[Id]=rm.[RoleId]
                    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=rm.[ApplicationModuleId]
                    INNER JOIN [dbo].[ModuleMenu] mm ON mm.[Id]=@ModuleMenuId AND mm.[ApplicationModuleId]=rm.[ApplicationModuleId]
                    WHERE rm.[RoleId]=@RoleId AND rm.[ApplicationModuleId]=@ApplicationModuleId
                      AND rm.[IsActive]=1 AND r.[IsActive]=1 AND am.[IsActive]=1 AND mm.[IsActive]=1)
                    THROW 50065,'The role, module assignment, or menu is inactive.',1;
                IF @ParentMenuId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] WITH(UPDLOCK,HOLDLOCK)
                    WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                      AND [ModuleMenuId]=@ParentMenuId AND [IsActive]=1)
                    THROW 50063,'Activate the parent menu assignment before activating this child menu.',1;
            END
            ELSE IF EXISTS(SELECT 1 FROM [dbo].[RoleModuleMenu] childAssignment WITH(UPDLOCK,HOLDLOCK)
                INNER JOIN [dbo].[ModuleMenu] childMenu ON childMenu.[Id]=childAssignment.[ModuleMenuId]
                  AND childMenu.[ApplicationModuleId]=childAssignment.[ApplicationModuleId]
                WHERE childAssignment.[RoleId]=@RoleId AND childAssignment.[ApplicationModuleId]=@ApplicationModuleId
                  AND childMenu.[ParentMenuId]=@ModuleMenuId AND childAssignment.[IsActive]=1)
                THROW 50066,'Deactivate the assigned child menus before deactivating their parent menu.',1;
            DECLARE @Old nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
                FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                  AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            UPDATE [dbo].[RoleModuleMenu] SET [IsActive]=@IsActive,[ModifiedAtUtc]=@Now,[ModifiedBy]=@ActorName
            WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId AND [ModuleMenuId]=@ModuleMenuId;
            DECLARE @New nvarchar(max)=(SELECT [RoleId],[ApplicationModuleId],[ModuleMenuId],[IsActive],[AssignedAtUtc],[AssignedBy],[ModifiedAtUtc],[ModifiedBy]
                FROM [dbo].[RoleModuleMenu] WHERE [RoleId]=@RoleId AND [ApplicationModuleId]=@ApplicationModuleId
                  AND [ModuleMenuId]=@ModuleMenuId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
            INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
            VALUES(N'RoleModuleMenu',N'Modified',CONCAT(N'{"RoleId":',@RoleId,N',"ApplicationModuleId":',@ApplicationModuleId,N',"ModuleMenuId":',@ModuleMenuId,N'}'),
                N'["IsActive","ModifiedAtUtc","ModifiedBy"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
            COMMIT;
            ;WITH MenuTree AS
            (
                SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(m.[Name] AS nvarchar(2000)) [Hierarchy]
                FROM [dbo].[ModuleMenu] m WHERE m.[ApplicationModuleId]=@ApplicationModuleId AND m.[ParentMenuId] IS NULL
                UNION ALL
                SELECT m.[Id],m.[ApplicationModuleId],m.[ParentMenuId],m.[Name],m.[DisplayOrder],CAST(CONCAT(t.[Hierarchy],N' / ',m.[Name]) AS nvarchar(2000))
                FROM [dbo].[ModuleMenu] m INNER JOIN MenuTree t ON t.[Id]=m.[ParentMenuId] AND t.[ApplicationModuleId]=m.[ApplicationModuleId]
            )
            SELECT x.[RoleId],r.[Name] [RoleName],x.[ApplicationModuleId],am.[Name] [ModuleName],x.[ModuleMenuId],t.[ParentMenuId],
                t.[Name] [MenuName],t.[Hierarchy] [MenuHierarchy],t.[DisplayOrder],x.[IsActive],x.[AssignedAtUtc],x.[AssignedBy],x.[ModifiedAtUtc],x.[ModifiedBy]
            FROM [dbo].[RoleModuleMenu] x INNER JOIN [dbo].[Role] r ON r.[Id]=x.[RoleId]
            INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=x.[ApplicationModuleId]
            INNER JOIN MenuTree t ON t.[Id]=x.[ModuleMenuId] AND t.[ApplicationModuleId]=x.[ApplicationModuleId]
            WHERE x.[RoleId]=@RoleId AND x.[ApplicationModuleId]=@ApplicationModuleId AND x.[ModuleMenuId]=@ModuleMenuId
            OPTION(MAXRECURSION 100);
        END;
        """;

    private static string SqlBatch(string sql) =>
        $"EXEC(N'{sql.Replace("'", "''", StringComparison.Ordinal)}');";
}
