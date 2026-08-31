using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeApprovalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalWorkflow_ListItem_PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalWorkflow_PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalNotification_PermitApprovalId_RecipientUserId",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.RenameColumn(
                name: "PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                newName: "SubjectTypeListItemId");

            migrationBuilder.AlterColumn<int>(
                name: "SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationModuleId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectedNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectedNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkflowCode",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                DECLARE @PermitModuleId int=(SELECT TOP(1) [Id] FROM [dbo].[ApplicationModule] WHERE [Code]=N'PERMIT');
                IF @PermitModuleId IS NULL THROW 50120,'The PERMIT application module is required before workflows can be generalized.',1;
                UPDATE w SET
                    [ApplicationModuleId]=@PermitModuleId,
                    [SubjectType]=N'PERMIT_TYPE',
                    [WorkflowCode]=CONCAT(N'PERMIT.PERMIT_TYPE.',LEFT(li.[SystemName],50),N'.',w.[Id]),
                    [PendingNotificationTitle]=N'{Reference} requires approval',
                    [PendingNotificationMessage]=N'{Reference} is waiting for level {Level} approval.',
                    [ApprovedNotificationTitle]=N'{Reference} was approved',
                    [ApprovedNotificationMessage]=N'{Reference} completed its approval workflow.',
                    [RejectedNotificationTitle]=N'{Reference} was rejected',
                    [RejectedNotificationMessage]=N'{Reference} was rejected at level {Level}.'
                FROM [dbo].[ApprovalWorkflow] w
                INNER JOIN [dbo].[ListItem] li ON li.[ListItemId]=w.[SubjectTypeListItemId];
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ApplicationModuleId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "PermitApprovalId",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventCode",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModuleCode",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkflowCode",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE n SET
                    [WorkflowCode]=w.[WorkflowCode],
                    [ModuleCode]=am.[Code],
                    [EntityType]=N'PERMIT_APPLICATION',
                    [EntityId]=CONVERT(nvarchar(100),pa.[PermitApplicationId]),
                    [EventCode]=N'APPROVAL_REQUESTED'
                FROM [dbo].[ApprovalNotification] n
                INNER JOIN [dbo].[PermitApproval] pa ON pa.[Id]=n.[PermitApprovalId]
                INNER JOIN [dbo].[PermitApplication] p ON p.[Id]=pa.[PermitApplicationId]
                CROSS APPLY
                (
                    SELECT TOP(1) candidate.*
                    FROM [dbo].[ApprovalWorkflow] candidate
                    WHERE candidate.[SubjectTypeListItemId]=p.[PermitTypeListItemId]
                      AND candidate.[SubjectType]=N'PERMIT_TYPE'
                    ORDER BY candidate.[IsActive] DESC,candidate.[UpdatedAtUtc] DESC,candidate.[Id] DESC
                ) w
                INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=w.[ApplicationModuleId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_ApplicationModuleId_SubjectType_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                columns: new[] { "ApplicationModuleId", "SubjectType", "SubjectTypeListItemId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "SubjectTypeListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_WorkflowCode",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "WorkflowCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_PermitApprovalId_RecipientUserId_EventCode",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "PermitApprovalId", "RecipientUserId", "EventCode" },
                unique: true,
                filter: "[PermitApprovalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_WorkflowCode_EntityType_EntityId_EventCode",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "WorkflowCode", "EntityType", "EntityId", "EventCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalWorkflow_ApplicationModule_ApplicationModuleId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "ApplicationModuleId",
                principalSchema: "dbo",
                principalTable: "ApplicationModule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalWorkflow_ListItem_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "SubjectTypeListItemId",
                principalSchema: "dbo",
                principalTable: "ListItem",
                principalColumn: "ListItemId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupGet]
                    @PageNumber int=1,@PageSize int=10,@SearchTerm nvarchar(200)=NULL,
                    @SortBy nvarchar(30)=N'updatedAtUtc',@SortDirection varchar(4)='desc',
                    @ApplicationModuleId int=NULL,@IncludeInactive bit=0
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SET @PageNumber=CASE WHEN @PageNumber<1 THEN 1 ELSE @PageNumber END;
                    SET @PageSize=CASE WHEN @PageSize<1 THEN 10 WHEN @PageSize>100 THEN 100 ELSE @PageSize END;
                    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'');
                    SET @SortBy=LOWER(COALESCE(@SortBy,N'updatedatutc'));
                    SET @SortDirection=LOWER(COALESCE(@SortDirection,'desc'));
                    SELECT w.[Id],w.[WorkflowCode],w.[ApplicationModuleId],am.[Name] [ModuleName],w.[SubjectType],
                        w.[SubjectTypeListItemId],li.[ItemName] [SubjectTypeName],w.[Name],
                        (SELECT COUNT(1) FROM [dbo].[ApprovalWorkflowLevel] l WHERE l.[ApprovalWorkflowId]=w.[Id]) [LevelCount],
                        w.[IsActive],w.[CreatedAtUtc],w.[UpdatedAtUtc]
                    INTO #Rows
                    FROM [dbo].[ApprovalWorkflow] w
                    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=w.[ApplicationModuleId]
                    LEFT JOIN [dbo].[ListItem] li ON li.[ListItemId]=w.[SubjectTypeListItemId]
                    WHERE (@IncludeInactive=1 OR w.[IsActive]=1)
                      AND (@ApplicationModuleId IS NULL OR w.[ApplicationModuleId]=@ApplicationModuleId)
                      AND (@SearchTerm IS NULL OR w.[WorkflowCode] LIKE N'%'+@SearchTerm+N'%'
                        OR am.[Name] LIKE N'%'+@SearchTerm+N'%' OR w.[SubjectType] LIKE N'%'+@SearchTerm+N'%'
                        OR li.[ItemName] LIKE N'%'+@SearchTerm+N'%' OR w.[Name] LIKE N'%'+@SearchTerm+N'%');
                    SELECT COUNT_BIG(1) [TotalRecords] FROM #Rows;
                    SELECT * FROM #Rows ORDER BY
                        CASE WHEN @SortBy=N'workflowcode' AND @SortDirection='asc' THEN [WorkflowCode] END ASC,
                        CASE WHEN @SortBy=N'workflowcode' AND @SortDirection='desc' THEN [WorkflowCode] END DESC,
                        CASE WHEN @SortBy=N'modulename' AND @SortDirection='asc' THEN [ModuleName] END ASC,
                        CASE WHEN @SortBy=N'modulename' AND @SortDirection='desc' THEN [ModuleName] END DESC,
                        CASE WHEN @SortBy=N'subjecttype' AND @SortDirection='asc' THEN [SubjectType] END ASC,
                        CASE WHEN @SortBy=N'subjecttype' AND @SortDirection='desc' THEN [SubjectType] END DESC,
                        CASE WHEN @SortBy=N'subjecttypename' AND @SortDirection='asc' THEN [SubjectTypeName] END ASC,
                        CASE WHEN @SortBy=N'subjecttypename' AND @SortDirection='desc' THEN [SubjectTypeName] END DESC,
                        CASE WHEN @SortBy=N'name' AND @SortDirection='asc' THEN [Name] END ASC,
                        CASE WHEN @SortBy=N'name' AND @SortDirection='desc' THEN [Name] END DESC,
                        CASE WHEN @SortBy=N'levelcount' AND @SortDirection='asc' THEN [LevelCount] END ASC,
                        CASE WHEN @SortBy=N'levelcount' AND @SortDirection='desc' THEN [LevelCount] END DESC,
                        CASE WHEN @SortBy=N'updatedatutc' AND @SortDirection='asc' THEN COALESCE([UpdatedAtUtc],[CreatedAtUtc]) END ASC,
                        CASE WHEN @SortBy=N'updatedatutc' AND @SortDirection='desc' THEN COALESCE([UpdatedAtUtc],[CreatedAtUtc]) END DESC,
                        [WorkflowCode]
                    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
                END;
                """));

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupGetById] @Id int
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT w.[Id],w.[ApplicationModuleId],am.[Code] [ModuleCode],am.[Name] [ModuleName],
                        w.[WorkflowCode],w.[SubjectType],w.[SubjectTypeListItemId],li.[ItemName] [SubjectTypeName],
                        w.[Name],w.[IsActive],w.[PendingNotificationTitle],w.[PendingNotificationMessage],
                        w.[ApprovedNotificationTitle],w.[ApprovedNotificationMessage],
                        w.[RejectedNotificationTitle],w.[RejectedNotificationMessage]
                    FROM [dbo].[ApprovalWorkflow] w
                    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=w.[ApplicationModuleId]
                    LEFT JOIN [dbo].[ListItem] li ON li.[ListItemId]=w.[SubjectTypeListItemId]
                    WHERE w.[Id]=@Id;
                    SELECT l.[Id],l.[LevelNumber],l.[PrimaryApproverRoleId],pr.[Name] [PrimaryApproverRoleName],
                        l.[AlternateApproverRoleId],ar.[Name] [AlternateApproverRoleName]
                    FROM [dbo].[ApprovalWorkflowLevel] l
                    INNER JOIN [dbo].[Role] pr ON pr.[Id]=l.[PrimaryApproverRoleId]
                    LEFT JOIN [dbo].[Role] ar ON ar.[Id]=l.[AlternateApproverRoleId]
                    WHERE l.[ApprovalWorkflowId]=@Id ORDER BY l.[LevelNumber];
                END;
                """));

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupModulesDdl] AS
                BEGIN SET NOCOUNT ON; SELECT [Id],[Code],[Name] FROM [dbo].[ApplicationModule]
                    WHERE [IsActive]=1 ORDER BY [DisplayOrder],[Name]; END;
                """));
            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupRolesDdl] AS
                BEGIN SET NOCOUNT ON; SELECT [Id],[Name] FROM [dbo].[Role] WHERE [IsActive]=1 ORDER BY [Name]; END;
                """));
            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupSubjectCategoriesDdl] AS
                BEGIN SET NOCOUNT ON; SELECT [ListItemCategoryId] [Id],[Code],[CategoryName] [Name]
                    FROM [dbo].[ListItemCategory] WHERE [IsActive]=1 ORDER BY [CategoryName],[ListItemCategoryId]; END;
                """));
            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupSubjectsDdl] @CategoryCode nvarchar(100) AS
                BEGIN SET NOCOUNT ON; SELECT i.[ListItemId] [Id],i.[SystemName] [Code],i.[ItemName] [Name]
                    FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
                    WHERE c.[Code]=@CategoryCode AND c.[IsActive]=1 AND i.[IsVisible]=1
                    ORDER BY i.[DisplayOrder],i.[ItemName]; END;
                """));

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupAdd]
                    @ApplicationModuleId int,@WorkflowCode nvarchar(100),@SubjectType nvarchar(100),
                    @SubjectTypeListItemId int=NULL,@Name nvarchar(150),@IsActive bit,@LevelsJson nvarchar(max),
                    @PendingTitle nvarchar(200),@PendingMessage nvarchar(1000),
                    @ApprovedTitle nvarchar(200),@ApprovedMessage nvarchar(1000),
                    @RejectedTitle nvarchar(200),@RejectedMessage nvarchar(1000),
                    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @WorkflowCode=UPPER(LTRIM(RTRIM(@WorkflowCode))); SET @SubjectType=UPPER(LTRIM(RTRIM(@SubjectType)));
                    SET @Name=LTRIM(RTRIM(@Name)); SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''),N'System');
                    IF LEN(@WorkflowCode)<3 OR @WorkflowCode LIKE N'%[^A-Z0-9._-]%' THROW 50130,'Workflow code is invalid.',1;
                    IF LEN(@Name)<2 THROW 50131,'Workflow name is required.',1;
                    IF ISJSON(@LevelsJson)<>1 THROW 50132,'Workflow levels are invalid.',1;
                    DECLARE @Levels TABLE(LevelNumber tinyint,PrimaryRoleId int,AlternateRoleId int NULL);
                    INSERT @Levels SELECT LevelNumber,PrimaryApproverRoleId,AlternateApproverRoleId FROM OPENJSON(@LevelsJson)
                        WITH(LevelNumber tinyint '$.LevelNumber',PrimaryApproverRoleId int '$.PrimaryApproverRoleId',AlternateApproverRoleId int '$.AlternateApproverRoleId');
                    IF (SELECT COUNT(1) FROM @Levels) NOT BETWEEN 1 AND 5 OR (SELECT MIN(LevelNumber) FROM @Levels)<>1
                        OR (SELECT MAX(LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
                        OR (SELECT COUNT(DISTINCT LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
                        THROW 50133,'Workflow levels must be sequential from 1 to 5.',1;
                    IF EXISTS(SELECT 1 FROM @Levels WHERE PrimaryRoleId=AlternateRoleId) THROW 50134,'Primary and alternate roles must differ.',1;
                    IF EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[PrimaryRoleId] AND r.[IsActive]=1 WHERE r.[Id] IS NULL)
                        OR EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[AlternateRoleId] AND r.[IsActive]=1 WHERE l.[AlternateRoleId] IS NOT NULL AND r.[Id] IS NULL)
                        THROW 50135,'Every approver role must be active.',1;
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApplicationModule] WHERE [Id]=@ApplicationModuleId AND [IsActive]=1)
                        THROW 50136,'The selected application module is inactive or missing.',1;
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WHERE [Code]=@SubjectType AND [IsActive]=1)
                        THROW 50137,'The selected workflow type/category is inactive or missing.',1;
                    IF @SubjectTypeListItemId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] i
                        INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
                        WHERE i.[ListItemId]=@SubjectTypeListItemId AND i.[IsVisible]=1 AND c.[Code]=@SubjectType)
                        THROW 50138,'The selected specific type does not belong to the workflow category.',1;
                    IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [WorkflowCode]=@WorkflowCode)
                        THROW 50139,'Workflow code already exists.',1;
                    IF @IsActive=1 AND EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [IsActive]=1
                        AND [ApplicationModuleId]=@ApplicationModuleId AND [SubjectType]=@SubjectType
                        AND ([SubjectTypeListItemId]=@SubjectTypeListItemId OR ([SubjectTypeListItemId] IS NULL AND @SubjectTypeListItemId IS NULL)))
                        THROW 50140,'An active workflow already exists for this module and type.',1;
                    IF @PendingTitle NOT LIKE N'%{Reference}%' OR @PendingMessage NOT LIKE N'%{Reference}%'
                        OR @ApprovedTitle NOT LIKE N'%{Reference}%' OR @ApprovedMessage NOT LIKE N'%{Reference}%'
                        OR @RejectedTitle NOT LIKE N'%{Reference}%' OR @RejectedMessage NOT LIKE N'%{Reference}%'
                        THROW 50141,'Every notification template must contain {Reference}.',1;
                    DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@Id int; BEGIN TRANSACTION;
                    INSERT [dbo].[ApprovalWorkflow]([ApplicationModuleId],[WorkflowCode],[SubjectType],[SubjectTypeListItemId],[Name],[IsActive],
                        [PendingNotificationTitle],[PendingNotificationMessage],[ApprovedNotificationTitle],[ApprovedNotificationMessage],
                        [RejectedNotificationTitle],[RejectedNotificationMessage],[CreatedByUserId],[CreatedAtUtc])
                    VALUES(@ApplicationModuleId,@WorkflowCode,@SubjectType,@SubjectTypeListItemId,@Name,@IsActive,
                        @PendingTitle,@PendingMessage,@ApprovedTitle,@ApprovedMessage,@RejectedTitle,@RejectedMessage,@ActorUserId,@Now);
                    SET @Id=SCOPE_IDENTITY();
                    INSERT [dbo].[ApprovalWorkflowLevel]([ApprovalWorkflowId],[LevelNumber],[PrimaryApproverRoleId],[AlternateApproverRoleId])
                        SELECT @Id,LevelNumber,PrimaryRoleId,AlternateRoleId FROM @Levels;
                    DECLARE @New nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
                    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                        VALUES(N'ApprovalWorkflow',N'Added',CONCAT(N'{"Id":',@Id,N'}'),N'["Workflow","Levels","NotificationTemplates"]',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                    COMMIT; SELECT @Id;
                END;
                """));

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupEdit]
                    @Id int,@ApplicationModuleId int,@WorkflowCode nvarchar(100),@SubjectType nvarchar(100),
                    @SubjectTypeListItemId int=NULL,@Name nvarchar(150),@IsActive bit,@LevelsJson nvarchar(max),
                    @PendingTitle nvarchar(200),@PendingMessage nvarchar(1000),
                    @ApprovedTitle nvarchar(200),@ApprovedMessage nvarchar(1000),
                    @RejectedTitle nvarchar(200),@RejectedMessage nvarchar(1000),
                    @ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON;
                    SET @WorkflowCode=UPPER(LTRIM(RTRIM(@WorkflowCode))); SET @SubjectType=UPPER(LTRIM(RTRIM(@SubjectType)));
                    SET @Name=LTRIM(RTRIM(@Name)); SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''),N'System');
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id) THROW 50004,'Workflow was not found.',1;
                    IF LEN(@WorkflowCode)<3 OR @WorkflowCode LIKE N'%[^A-Z0-9._-]%' THROW 50130,'Workflow code is invalid.',1;
                    IF LEN(@Name)<2 OR ISJSON(@LevelsJson)<>1 THROW 50132,'Workflow name or levels are invalid.',1;
                    DECLARE @Levels TABLE(LevelNumber tinyint,PrimaryRoleId int,AlternateRoleId int NULL);
                    INSERT @Levels SELECT LevelNumber,PrimaryApproverRoleId,AlternateApproverRoleId FROM OPENJSON(@LevelsJson)
                        WITH(LevelNumber tinyint '$.LevelNumber',PrimaryApproverRoleId int '$.PrimaryApproverRoleId',AlternateApproverRoleId int '$.AlternateApproverRoleId');
                    IF (SELECT COUNT(1) FROM @Levels) NOT BETWEEN 1 AND 5 OR (SELECT MIN(LevelNumber) FROM @Levels)<>1
                        OR (SELECT MAX(LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
                        OR (SELECT COUNT(DISTINCT LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
                        THROW 50133,'Workflow levels must be sequential from 1 to 5.',1;
                    IF EXISTS(SELECT 1 FROM @Levels WHERE PrimaryRoleId=AlternateRoleId) THROW 50134,'Primary and alternate roles must differ.',1;
                    IF EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[PrimaryRoleId] AND r.[IsActive]=1 WHERE r.[Id] IS NULL)
                        OR EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[AlternateRoleId] AND r.[IsActive]=1 WHERE l.[AlternateRoleId] IS NOT NULL AND r.[Id] IS NULL)
                        THROW 50135,'Every approver role must be active.',1;
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApplicationModule] WHERE [Id]=@ApplicationModuleId AND [IsActive]=1)
                        THROW 50136,'The selected application module is inactive or missing.',1;
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WHERE [Code]=@SubjectType AND [IsActive]=1)
                        THROW 50137,'The selected workflow type/category is inactive or missing.',1;
                    IF @SubjectTypeListItemId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] i
                        INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
                        WHERE i.[ListItemId]=@SubjectTypeListItemId AND i.[IsVisible]=1 AND c.[Code]=@SubjectType)
                        THROW 50138,'The selected specific type does not belong to the workflow category.',1;
                    IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]<>@Id AND [WorkflowCode]=@WorkflowCode)
                        THROW 50139,'Workflow code already exists.',1;
                    IF @IsActive=1 AND EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]<>@Id AND [IsActive]=1
                        AND [ApplicationModuleId]=@ApplicationModuleId AND [SubjectType]=@SubjectType
                        AND ([SubjectTypeListItemId]=@SubjectTypeListItemId OR ([SubjectTypeListItemId] IS NULL AND @SubjectTypeListItemId IS NULL)))
                        THROW 50140,'An active workflow already exists for this module and type.',1;
                    IF @PendingTitle NOT LIKE N'%{Reference}%' OR @PendingMessage NOT LIKE N'%{Reference}%'
                        OR @ApprovedTitle NOT LIKE N'%{Reference}%' OR @ApprovedMessage NOT LIKE N'%{Reference}%'
                        OR @RejectedTitle NOT LIKE N'%{Reference}%' OR @RejectedMessage NOT LIKE N'%{Reference}%'
                        THROW 50141,'Every notification template must contain {Reference}.',1;
                    DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
                    DECLARE @Old nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
                    UPDATE [dbo].[ApprovalWorkflow] SET [ApplicationModuleId]=@ApplicationModuleId,
                        [WorkflowCode]=@WorkflowCode,[SubjectType]=@SubjectType,[SubjectTypeListItemId]=@SubjectTypeListItemId,
                        [Name]=@Name,[IsActive]=@IsActive,
                        [PendingNotificationTitle]=@PendingTitle,[PendingNotificationMessage]=@PendingMessage,
                        [ApprovedNotificationTitle]=@ApprovedTitle,[ApprovedNotificationMessage]=@ApprovedMessage,
                        [RejectedNotificationTitle]=@RejectedTitle,[RejectedNotificationMessage]=@RejectedMessage,[UpdatedAtUtc]=@Now WHERE [Id]=@Id;
                    DELETE [dbo].[ApprovalWorkflowLevel] WHERE [ApprovalWorkflowId]=@Id;
                    INSERT [dbo].[ApprovalWorkflowLevel]([ApprovalWorkflowId],[LevelNumber],[PrimaryApproverRoleId],[AlternateApproverRoleId])
                        SELECT @Id,LevelNumber,PrimaryRoleId,AlternateRoleId FROM @Levels;
                    DECLARE @New nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
                    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                        VALUES(N'ApprovalWorkflow',N'Modified',CONCAT(N'{"Id":',@Id,N'}'),N'["Workflow","Levels","NotificationTemplates"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                    COMMIT; SELECT @Id;
                END;
                """));

            migrationBuilder.Sql(SqlBatch("""
                CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupDelete]
                    @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
                AS
                BEGIN
                    SET NOCOUNT ON; SET XACT_ABORT ON; SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''),N'System');
                    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id) BEGIN SELECT CAST(0 AS int); RETURN; END;
                    DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@Old nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
                    UPDATE [dbo].[ApprovalWorkflow] SET [IsActive]=0,[UpdatedAtUtc]=@Now WHERE [Id]=@Id;
                    DECLARE @New nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
                    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
                        VALUES(N'ApprovalWorkflow',N'Deactivated',CONCAT(N'{"Id":',@Id,N'}'),N'["IsActive","UpdatedAtUtc"]',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
                    SELECT CAST(1 AS int);
                END;
                """));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupDelete];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupEdit];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupAdd];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupSubjectsDdl];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupSubjectCategoriesDdl];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupRolesDdl];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupModulesDdl];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupGetById];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpWorkflowSetupGet];");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalWorkflow_ApplicationModule_ApplicationModuleId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalWorkflow_ListItem_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalWorkflow_ApplicationModuleId_SubjectType_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalWorkflow_SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalWorkflow_WorkflowCode",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalNotification_PermitApprovalId_RecipientUserId_EventCode",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalNotification_WorkflowCode_EntityType_EntityId_EventCode",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.Sql(
                """
                IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] w
                    INNER JOIN [dbo].[ApplicationModule] am ON am.[Id]=w.[ApplicationModuleId]
                    WHERE am.[Code]<>N'PERMIT' OR w.[SubjectType]<>N'PERMIT_TYPE' OR w.[SubjectTypeListItemId] IS NULL)
                    THROW 50121,'Generic non-permit workflows must be removed before rolling back this migration.',1;
                IF EXISTS(SELECT 1 FROM [dbo].[ApprovalNotification] WHERE [PermitApprovalId] IS NULL)
                    THROW 50122,'Generic notifications must be removed before rolling back this migration.',1;
                """);

            migrationBuilder.DropColumn(
                name: "ApprovedNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "ApprovedNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "PendingNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "PendingNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "RejectedNotificationMessage",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "RejectedNotificationTitle",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "WorkflowCode",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "ApplicationModuleId",
                schema: "dbo",
                table: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "EntityId",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.DropColumn(
                name: "EntityType",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.DropColumn(
                name: "EventCode",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.DropColumn(
                name: "ModuleCode",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.DropColumn(
                name: "WorkflowCode",
                schema: "dbo",
                table: "ApprovalNotification");

            migrationBuilder.AlterColumn<int>(
                name: "SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "SubjectTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                newName: "PermitTypeListItemId");

            migrationBuilder.AlterColumn<long>(
                name: "PermitApprovalId",
                schema: "dbo",
                table: "ApprovalNotification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "PermitTypeListItemId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotification_PermitApprovalId_RecipientUserId",
                schema: "dbo",
                table: "ApprovalNotification",
                columns: new[] { "PermitApprovalId", "RecipientUserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalWorkflow_ListItem_PermitTypeListItemId",
                schema: "dbo",
                table: "ApprovalWorkflow",
                column: "PermitTypeListItemId",
                principalSchema: "dbo",
                principalTable: "ListItem",
                principalColumn: "ListItemId",
                onDelete: ReferentialAction.Restrict);
        }

        private static string SqlBatch(string sql) =>
            $"EXEC(N'{sql.Replace("'", "''", StringComparison.Ordinal)}');";
    }
}
