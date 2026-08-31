BEGIN TRANSACTION;
ALTER TABLE [dbo].[ApprovalWorkflow] DROP CONSTRAINT [FK_ApprovalWorkflow_ListItem_PermitTypeListItemId];

DROP INDEX [IX_ApprovalWorkflow_PermitTypeListItemId] ON [dbo].[ApprovalWorkflow];

DROP INDEX [IX_ApprovalNotification_PermitApprovalId_RecipientUserId] ON [dbo].[ApprovalNotification];

EXEC sp_rename N'[dbo].[ApprovalWorkflow].[PermitTypeListItemId]', N'SubjectTypeListItemId', 'COLUMN';

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ApprovalWorkflow]') AND [c].[name] = N'SubjectTypeListItemId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ApprovalWorkflow] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [dbo].[ApprovalWorkflow] ALTER COLUMN [SubjectTypeListItemId] int NULL;

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [ApplicationModuleId] int NULL;

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [ApprovedNotificationMessage] nvarchar(1000) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [ApprovedNotificationTitle] nvarchar(200) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [PendingNotificationMessage] nvarchar(1000) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [PendingNotificationTitle] nvarchar(200) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [RejectedNotificationMessage] nvarchar(1000) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [RejectedNotificationTitle] nvarchar(200) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [SubjectType] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalWorkflow] ADD [WorkflowCode] nvarchar(100) NOT NULL DEFAULT N'';

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

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ApprovalWorkflow]') AND [c].[name] = N'ApplicationModuleId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ApprovalWorkflow] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [dbo].[ApprovalWorkflow] ALTER COLUMN [ApplicationModuleId] int NOT NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[ApprovalNotification]') AND [c].[name] = N'PermitApprovalId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ApprovalNotification] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [dbo].[ApprovalNotification] ALTER COLUMN [PermitApprovalId] bigint NULL;

ALTER TABLE [dbo].[ApprovalNotification] ADD [EntityId] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalNotification] ADD [EntityType] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalNotification] ADD [EventCode] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalNotification] ADD [ModuleCode] nvarchar(30) NOT NULL DEFAULT N'';

ALTER TABLE [dbo].[ApprovalNotification] ADD [WorkflowCode] nvarchar(100) NOT NULL DEFAULT N'';

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

CREATE UNIQUE INDEX [IX_ApprovalWorkflow_ApplicationModuleId_SubjectType_SubjectTypeListItemId] ON [dbo].[ApprovalWorkflow] ([ApplicationModuleId], [SubjectType], [SubjectTypeListItemId]) WHERE [IsActive] = 1;

CREATE INDEX [IX_ApprovalWorkflow_SubjectTypeListItemId] ON [dbo].[ApprovalWorkflow] ([SubjectTypeListItemId]);

CREATE UNIQUE INDEX [IX_ApprovalWorkflow_WorkflowCode] ON [dbo].[ApprovalWorkflow] ([WorkflowCode]);

CREATE UNIQUE INDEX [IX_ApprovalNotification_PermitApprovalId_RecipientUserId_EventCode] ON [dbo].[ApprovalNotification] ([PermitApprovalId], [RecipientUserId], [EventCode]) WHERE [PermitApprovalId] IS NOT NULL;

CREATE INDEX [IX_ApprovalNotification_WorkflowCode_EntityType_EntityId_EventCode] ON [dbo].[ApprovalNotification] ([WorkflowCode], [EntityType], [EntityId], [EventCode]);

ALTER TABLE [dbo].[ApprovalWorkflow] ADD CONSTRAINT [FK_ApprovalWorkflow_ApplicationModule_ApplicationModuleId] FOREIGN KEY ([ApplicationModuleId]) REFERENCES [dbo].[ApplicationModule] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [dbo].[ApprovalWorkflow] ADD CONSTRAINT [FK_ApprovalWorkflow_ListItem_SubjectTypeListItemId] FOREIGN KEY ([SubjectTypeListItemId]) REFERENCES [dbo].[ListItem] ([ListItemId]) ON DELETE NO ACTION;

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupGet]
    @PageNumber int=1,@PageSize int=10,@SearchTerm nvarchar(200)=NULL,
    @SortBy nvarchar(30)=N''updatedAtUtc'',@SortDirection varchar(4)=''desc'',
    @ApplicationModuleId int=NULL,@IncludeInactive bit=0
AS
BEGIN
    SET NOCOUNT ON;
    SET @PageNumber=CASE WHEN @PageNumber<1 THEN 1 ELSE @PageNumber END;
    SET @PageSize=CASE WHEN @PageSize<1 THEN 10 WHEN @PageSize>100 THEN 100 ELSE @PageSize END;
    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'''');
    SET @SortBy=LOWER(COALESCE(@SortBy,N''updatedatutc''));
    SET @SortDirection=LOWER(COALESCE(@SortDirection,''desc''));
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
      AND (@SearchTerm IS NULL OR w.[WorkflowCode] LIKE N''%''+@SearchTerm+N''%''
        OR am.[Name] LIKE N''%''+@SearchTerm+N''%'' OR w.[SubjectType] LIKE N''%''+@SearchTerm+N''%''
        OR li.[ItemName] LIKE N''%''+@SearchTerm+N''%'' OR w.[Name] LIKE N''%''+@SearchTerm+N''%'');
    SELECT COUNT_BIG(1) [TotalRecords] FROM #Rows;
    SELECT * FROM #Rows ORDER BY
        CASE WHEN @SortBy=N''workflowcode'' AND @SortDirection=''asc'' THEN [WorkflowCode] END ASC,
        CASE WHEN @SortBy=N''workflowcode'' AND @SortDirection=''desc'' THEN [WorkflowCode] END DESC,
        CASE WHEN @SortBy=N''modulename'' AND @SortDirection=''asc'' THEN [ModuleName] END ASC,
        CASE WHEN @SortBy=N''modulename'' AND @SortDirection=''desc'' THEN [ModuleName] END DESC,
        CASE WHEN @SortBy=N''subjecttype'' AND @SortDirection=''asc'' THEN [SubjectType] END ASC,
        CASE WHEN @SortBy=N''subjecttype'' AND @SortDirection=''desc'' THEN [SubjectType] END DESC,
        CASE WHEN @SortBy=N''subjecttypename'' AND @SortDirection=''asc'' THEN [SubjectTypeName] END ASC,
        CASE WHEN @SortBy=N''subjecttypename'' AND @SortDirection=''desc'' THEN [SubjectTypeName] END DESC,
        CASE WHEN @SortBy=N''name'' AND @SortDirection=''asc'' THEN [Name] END ASC,
        CASE WHEN @SortBy=N''name'' AND @SortDirection=''desc'' THEN [Name] END DESC,
        CASE WHEN @SortBy=N''levelcount'' AND @SortDirection=''asc'' THEN [LevelCount] END ASC,
        CASE WHEN @SortBy=N''levelcount'' AND @SortDirection=''desc'' THEN [LevelCount] END DESC,
        CASE WHEN @SortBy=N''updatedatutc'' AND @SortDirection=''asc'' THEN COALESCE([UpdatedAtUtc],[CreatedAtUtc]) END ASC,
        CASE WHEN @SortBy=N''updatedatutc'' AND @SortDirection=''desc'' THEN COALESCE([UpdatedAtUtc],[CreatedAtUtc]) END DESC,
        [WorkflowCode]
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupGetById] @Id int
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
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupModulesDdl] AS
BEGIN SET NOCOUNT ON; SELECT [Id],[Code],[Name] FROM [dbo].[ApplicationModule]
    WHERE [IsActive]=1 ORDER BY [DisplayOrder],[Name]; END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupRolesDdl] AS
BEGIN SET NOCOUNT ON; SELECT [Id],[Name] FROM [dbo].[Role] WHERE [IsActive]=1 ORDER BY [Name]; END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupSubjectCategoriesDdl] AS
BEGIN SET NOCOUNT ON; SELECT [ListItemCategoryId] [Id],[Code],[CategoryName] [Name]
    FROM [dbo].[ListItemCategory] WHERE [IsActive]=1 ORDER BY [CategoryName],[ListItemCategoryId]; END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupSubjectsDdl] @CategoryCode nvarchar(100) AS
BEGIN SET NOCOUNT ON; SELECT i.[ListItemId] [Id],i.[SystemName] [Code],i.[ItemName] [Name]
    FROM [dbo].[ListItem] i INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
    WHERE c.[Code]=@CategoryCode AND c.[IsActive]=1 AND i.[IsVisible]=1
    ORDER BY i.[DisplayOrder],i.[ItemName]; END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupAdd]
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
    SET @Name=LTRIM(RTRIM(@Name)); SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    IF LEN(@WorkflowCode)<3 OR @WorkflowCode LIKE N''%[^A-Z0-9._-]%'' THROW 50130,''Workflow code is invalid.'',1;
    IF LEN(@Name)<2 THROW 50131,''Workflow name is required.'',1;
    IF ISJSON(@LevelsJson)<>1 THROW 50132,''Workflow levels are invalid.'',1;
    DECLARE @Levels TABLE(LevelNumber tinyint,PrimaryRoleId int,AlternateRoleId int NULL);
    INSERT @Levels SELECT LevelNumber,PrimaryApproverRoleId,AlternateApproverRoleId FROM OPENJSON(@LevelsJson)
        WITH(LevelNumber tinyint ''$.LevelNumber'',PrimaryApproverRoleId int ''$.PrimaryApproverRoleId'',AlternateApproverRoleId int ''$.AlternateApproverRoleId'');
    IF (SELECT COUNT(1) FROM @Levels) NOT BETWEEN 1 AND 5 OR (SELECT MIN(LevelNumber) FROM @Levels)<>1
        OR (SELECT MAX(LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
        OR (SELECT COUNT(DISTINCT LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
        THROW 50133,''Workflow levels must be sequential from 1 to 5.'',1;
    IF EXISTS(SELECT 1 FROM @Levels WHERE PrimaryRoleId=AlternateRoleId) THROW 50134,''Primary and alternate roles must differ.'',1;
    IF EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[PrimaryRoleId] AND r.[IsActive]=1 WHERE r.[Id] IS NULL)
        OR EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[AlternateRoleId] AND r.[IsActive]=1 WHERE l.[AlternateRoleId] IS NOT NULL AND r.[Id] IS NULL)
        THROW 50135,''Every approver role must be active.'',1;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApplicationModule] WHERE [Id]=@ApplicationModuleId AND [IsActive]=1)
        THROW 50136,''The selected application module is inactive or missing.'',1;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WHERE [Code]=@SubjectType AND [IsActive]=1)
        THROW 50137,''The selected workflow type/category is inactive or missing.'',1;
    IF @SubjectTypeListItemId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] i
        INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
        WHERE i.[ListItemId]=@SubjectTypeListItemId AND i.[IsVisible]=1 AND c.[Code]=@SubjectType)
        THROW 50138,''The selected specific type does not belong to the workflow category.'',1;
    IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [WorkflowCode]=@WorkflowCode)
        THROW 50139,''Workflow code already exists.'',1;
    IF @IsActive=1 AND EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [IsActive]=1
        AND [ApplicationModuleId]=@ApplicationModuleId AND [SubjectType]=@SubjectType
        AND ([SubjectTypeListItemId]=@SubjectTypeListItemId OR ([SubjectTypeListItemId] IS NULL AND @SubjectTypeListItemId IS NULL)))
        THROW 50140,''An active workflow already exists for this module and type.'',1;
    IF @PendingTitle NOT LIKE N''%{Reference}%'' OR @PendingMessage NOT LIKE N''%{Reference}%''
        OR @ApprovedTitle NOT LIKE N''%{Reference}%'' OR @ApprovedMessage NOT LIKE N''%{Reference}%''
        OR @RejectedTitle NOT LIKE N''%{Reference}%'' OR @RejectedMessage NOT LIKE N''%{Reference}%''
        THROW 50141,''Every notification template must contain {Reference}.'',1;
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
        VALUES(N''ApprovalWorkflow'',N''Added'',CONCAT(N''{"Id":'',@Id,N''}''),N''["Workflow","Levels","NotificationTemplates"]'',@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT; SELECT @Id;
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupEdit]
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
    SET @Name=LTRIM(RTRIM(@Name)); SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id) THROW 50004,''Workflow was not found.'',1;
    IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id AND
        ([ApplicationModuleId]<>@ApplicationModuleId OR [WorkflowCode]<>@WorkflowCode OR [SubjectType]<>@SubjectType
         OR NOT([SubjectTypeListItemId]=@SubjectTypeListItemId OR ([SubjectTypeListItemId] IS NULL AND @SubjectTypeListItemId IS NULL))))
        THROW 50142,''Module, workflow code, and workflow type cannot be changed after creation.'',1;
    IF LEN(@Name)<2 OR ISJSON(@LevelsJson)<>1 THROW 50132,''Workflow name or levels are invalid.'',1;
    DECLARE @Levels TABLE(LevelNumber tinyint,PrimaryRoleId int,AlternateRoleId int NULL);
    INSERT @Levels SELECT LevelNumber,PrimaryApproverRoleId,AlternateApproverRoleId FROM OPENJSON(@LevelsJson)
        WITH(LevelNumber tinyint ''$.LevelNumber'',PrimaryApproverRoleId int ''$.PrimaryApproverRoleId'',AlternateApproverRoleId int ''$.AlternateApproverRoleId'');
    IF (SELECT COUNT(1) FROM @Levels) NOT BETWEEN 1 AND 5 OR (SELECT MIN(LevelNumber) FROM @Levels)<>1
        OR (SELECT MAX(LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
        OR (SELECT COUNT(DISTINCT LevelNumber) FROM @Levels)<>(SELECT COUNT(1) FROM @Levels)
        THROW 50133,''Workflow levels must be sequential from 1 to 5.'',1;
    IF EXISTS(SELECT 1 FROM @Levels WHERE PrimaryRoleId=AlternateRoleId) THROW 50134,''Primary and alternate roles must differ.'',1;
    IF EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[PrimaryRoleId] AND r.[IsActive]=1 WHERE r.[Id] IS NULL)
        OR EXISTS(SELECT 1 FROM @Levels l LEFT JOIN [dbo].[Role] r ON r.[Id]=l.[AlternateRoleId] AND r.[IsActive]=1 WHERE l.[AlternateRoleId] IS NOT NULL AND r.[Id] IS NULL)
        THROW 50135,''Every approver role must be active.'',1;
    IF @IsActive=1 AND EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]<>@Id AND [IsActive]=1
        AND [ApplicationModuleId]=@ApplicationModuleId AND [SubjectType]=@SubjectType
        AND ([SubjectTypeListItemId]=@SubjectTypeListItemId OR ([SubjectTypeListItemId] IS NULL AND @SubjectTypeListItemId IS NULL)))
        THROW 50140,''An active workflow already exists for this module and type.'',1;
    IF @PendingTitle NOT LIKE N''%{Reference}%'' OR @PendingMessage NOT LIKE N''%{Reference}%''
        OR @ApprovedTitle NOT LIKE N''%{Reference}%'' OR @ApprovedMessage NOT LIKE N''%{Reference}%''
        OR @RejectedTitle NOT LIKE N''%{Reference}%'' OR @RejectedMessage NOT LIKE N''%{Reference}%''
        THROW 50141,''Every notification template must contain {Reference}.'',1;
    DECLARE @Now datetime2(0)=SYSUTCDATETIME(); BEGIN TRANSACTION;
    DECLARE @Old nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    UPDATE [dbo].[ApprovalWorkflow] SET [Name]=@Name,[IsActive]=@IsActive,
        [PendingNotificationTitle]=@PendingTitle,[PendingNotificationMessage]=@PendingMessage,
        [ApprovedNotificationTitle]=@ApprovedTitle,[ApprovedNotificationMessage]=@ApprovedMessage,
        [RejectedNotificationTitle]=@RejectedTitle,[RejectedNotificationMessage]=@RejectedMessage,[UpdatedAtUtc]=@Now WHERE [Id]=@Id;
    DELETE [dbo].[ApprovalWorkflowLevel] WHERE [ApprovalWorkflowId]=@Id;
    INSERT [dbo].[ApprovalWorkflowLevel]([ApprovalWorkflowId],[LevelNumber],[PrimaryApproverRoleId],[AlternateApproverRoleId])
        SELECT @Id,LevelNumber,PrimaryRoleId,AlternateRoleId FROM @Levels;
    DECLARE @New nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
        VALUES(N''ApprovalWorkflow'',N''Modified'',CONCAT(N''{"Id":'',@Id,N''}''),N''["Workflow","Levels","NotificationTemplates"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT; SELECT @Id;
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpWorkflowSetupDelete]
    @Id int,@ActorUserId int=NULL,@ActorName nvarchar(256)=NULL,@TraceId nvarchar(100)=NULL,@IpAddress nvarchar(45)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; SET @ActorName=COALESCE(NULLIF(LTRIM(RTRIM(@ActorName)),N''''),N''System'');
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id) BEGIN SELECT CAST(0 AS int); RETURN; END;
    DECLARE @Now datetime2(0)=SYSUTCDATETIME(),@Old nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    UPDATE [dbo].[ApprovalWorkflow] SET [IsActive]=0,[UpdatedAtUtc]=@Now WHERE [Id]=@Id;
    DECLARE @New nvarchar(max)=(SELECT * FROM [dbo].[ApprovalWorkflow] WHERE [Id]=@Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
    INSERT [dbo].[AuditLog]([EntityName],[Action],[EntityKey],[ChangedColumns],[OldValues],[NewValues],[ChangedByUserId],[ChangedBy],[TraceId],[IpAddress],[ChangedAtUtc])
        VALUES(N''ApprovalWorkflow'',N''Deactivated'',CONCAT(N''{"Id":'',@Id,N''}''),N''["IsActive","UpdatedAtUtc"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    SELECT CAST(1 AS int);
END;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260831182159_GeneralizeApprovalWorkflows', N'10.0.9');

COMMIT;
GO

