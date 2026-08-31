BEGIN TRANSACTION;
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
    IF LEN(@WorkflowCode)<3 OR @WorkflowCode LIKE N''%[^A-Z0-9._-]%'' THROW 50130,''Workflow code is invalid.'',1;
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
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ApplicationModule] WHERE [Id]=@ApplicationModuleId AND [IsActive]=1)
        THROW 50136,''The selected application module is inactive or missing.'',1;
    IF NOT EXISTS(SELECT 1 FROM [dbo].[ListItemCategory] WHERE [Code]=@SubjectType AND [IsActive]=1)
        THROW 50137,''The selected workflow type/category is inactive or missing.'',1;
    IF @SubjectTypeListItemId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[ListItem] i
        INNER JOIN [dbo].[ListItemCategory] c ON c.[ListItemCategoryId]=i.[ListItemCategoryId]
        WHERE i.[ListItemId]=@SubjectTypeListItemId AND i.[IsVisible]=1 AND c.[Code]=@SubjectType)
        THROW 50138,''The selected specific type does not belong to the workflow category.'',1;
    IF EXISTS(SELECT 1 FROM [dbo].[ApprovalWorkflow] WHERE [Id]<>@Id AND [WorkflowCode]=@WorkflowCode)
        THROW 50139,''Workflow code already exists.'',1;
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
        VALUES(N''ApprovalWorkflow'',N''Modified'',CONCAT(N''{"Id":'',@Id,N''}''),N''["Workflow","Levels","NotificationTemplates"]'',@Old,@New,@ActorUserId,@ActorName,@TraceId,@IpAddress,@Now);
    COMMIT; SELECT @Id;
END;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260831191739_EnableWorkflowScopeEditing', N'10.0.9');

COMMIT;
GO

