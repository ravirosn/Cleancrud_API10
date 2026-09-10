BEGIN TRANSACTION;
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationUpd]
    @PermitApplicationId bigint,
    @IssueDate date,
    @PermitIssuerId int,
    @PermitIssuerContactNumber nvarchar(30)=NULL,
    @PermitReceiverId int,
    @PermitReceiverContactNumber nvarchar(30)=NULL,
    @WorkLocation nvarchar(500),
    @WorkDescription nvarchar(max),
    @SpecialInstructions nvarchar(max)=NULL,
    @WorkHeightBelowSurface nvarchar(200)=NULL,
    @CompletionOfWorks nvarchar(500)=NULL,
    @UpdatedByUserId int,
    @FinalizeForApproval bit=0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatusCode nvarchar(50), @CurrentStatusId int,
            @PermitTypeSystemName nvarchar(50), @FinalStatusId int,
            @FinalStatusCode nvarchar(50), @Now datetime2(0)=SYSUTCDATETIME();

    SELECT @CurrentStatusCode=permitStatus.[SystemName],
           @CurrentStatusId=permitApplication.[PermitStatusListItemId],
           @PermitTypeSystemName=permitType.[SystemName]
    FROM [dbo].[PermitApplication] permitApplication WITH (UPDLOCK)
    INNER JOIN [dbo].[ListItem] permitStatus
        ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[ListItem] permitType
        ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
    WHERE permitApplication.[Id]=@PermitApplicationId;

    IF @CurrentStatusCode IS NULL THROW 50001, ''Permit application was not found.'', 1;
    IF @CurrentStatusCode NOT IN (N''PERMIT_DRAFT'',N''PERMIT_REJECTED'',N''FINALIZED_FOR_APPROVAL'')
        THROW 50002, ''Only Draft, Rejected, or Finalized For Approval permit applications can be edited.'', 1;
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id]=@PermitIssuerId AND [IsActive]=1)
       OR NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id]=@PermitReceiverId AND [IsActive]=1)
        THROW 50005, ''Issuer and receiver must be active users.'', 1;
    SELECT @PermitIssuerContactNumber=[ContactNumber]
    FROM [dbo].[Users] WHERE [Id]=@PermitIssuerId;
    SELECT @PermitReceiverContactNumber=[ContactNumber]
    FROM [dbo].[Users] WHERE [Id]=@PermitReceiverId;

    SET @FinalStatusId=@CurrentStatusId;
    SET @FinalStatusCode=@CurrentStatusCode;
    IF @FinalizeForApproval=1
    BEGIN
        SELECT @FinalStatusId=item.[ListItemId],@FinalStatusCode=item.[SystemName]
        FROM [dbo].[ListItem] item
        INNER JOIN [dbo].[ListItemCategory] category
            ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
        WHERE category.[Code]=N''PERMIT_STATUS''
          AND item.[SystemName]=N''FINALIZED_FOR_APPROVAL'' AND item.[IsVisible]=1;
        IF @FinalStatusId IS NULL
            THROW 50006, ''The active FINALIZED_FOR_APPROVAL permit status is not configured.'', 1;
    END;

    UPDATE [dbo].[PermitApplication]
    SET [IssueDate]=@IssueDate,
        [PermitIssuerId]=@PermitIssuerId,
        [PermitIssuerContactNumber]=NULLIF(LTRIM(RTRIM(@PermitIssuerContactNumber)),N''''),
        [PermitReceiverId]=@PermitReceiverId,
        [PermitReceiverContactNumber]=NULLIF(LTRIM(RTRIM(@PermitReceiverContactNumber)),N''''),
        [WorkLocation]=LTRIM(RTRIM(@WorkLocation)),
        [WorkDescription]=LTRIM(RTRIM(@WorkDescription)),
        [SpecialInstructions]=NULLIF(LTRIM(RTRIM(@SpecialInstructions)),N''''),
        [WorkHeightBelowSurface]=NULLIF(LTRIM(RTRIM(@WorkHeightBelowSurface)),N''''),
        [CompletionOfWorks]=NULLIF(LTRIM(RTRIM(@CompletionOfWorks)),N''''),
        [PermitStatusListItemId]=@FinalStatusId,
        [UpdatedByUserId]=@UpdatedByUserId,
        [UpdatedAtUtc]=@Now
    WHERE [Id]=@PermitApplicationId;

    INSERT [dbo].[AuditLog]
        ([EntityName],[Action],[EntityKey],[ChangedColumns],[ChangedByUserId],[ChangedAtUtc])
    VALUES
        (N''PermitApplication'',N''UPDATE'',CONCAT(N''{"Id":'',@PermitApplicationId,N''}''),
         N''IssueDate,PermitIssuerId,PermitReceiverId,WorkLocation,WorkDescription,PermitTypeExtensions'',
         @UpdatedByUserId,@Now);

    SELECT @PermitApplicationId [PermitApplicationId], @FinalStatusId [PermitStatusListItemId],
           @FinalStatusCode [PermitStatusSystemName], @PermitTypeSystemName [PermitTypeSystemName],
           @Now [UpdatedAtUtc];
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationPreviewGet]
    @PermitApplicationId bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT permitApplication.[Id],permitApplication.[RiskAssessmentId],permitApplication.[PermitNumber],
           permitApplication.[IssueDate],permitApplication.[PermitIssuerId],
           COALESCE(issuer.[DisplayName],issuer.[UserName]) [PermitIssuerName],
           issuer.[ContactNumber] [PermitIssuerContactNumber],permitApplication.[PermitReceiverId],
           COALESCE(receiver.[DisplayName],receiver.[UserName]) [PermitReceiverName],
           receiver.[ContactNumber] [PermitReceiverContactNumber],permitApplication.[RiskAssessmentNumber],
           permitApplication.[WorkLocation],permitApplication.[WorkDescription],
           permitApplication.[SpecialInstructions],permitApplication.[WorkHeightBelowSurface],
           permitApplication.[PermitTypeListItemId],permitType.[SystemName] [PermitTypeSystemName],
           permitType.[ItemName] [PermitTypeName],permitApplication.[PermitStatusListItemId],
           permitStatus.[SystemName] [PermitStatusSystemName],permitStatus.[ItemName] [PermitStatusName],
           permitApplication.[SubmittedAtUtc],permitApplication.[CreatedByUserId],
           permitApplication.[UpdatedByUserId],permitApplication.[CreatedAtUtc],permitApplication.[UpdatedAtUtc],
           permitApplication.[CompletionOfWorks],permitApplication.[CompletionApprovedBy],
           permitApplication.[CompletionDate],permitApplication.[CompletionRemarks],
           permitApplication.[CancelledBy],permitApplication.[CancelledDate],permitApplication.[CancelledRemarks]
    FROM [dbo].[PermitApplication] permitApplication
    INNER JOIN [dbo].[Users] issuer ON issuer.[Id]=permitApplication.[PermitIssuerId]
    INNER JOIN [dbo].[Users] receiver ON receiver.[Id]=permitApplication.[PermitReceiverId]
    INNER JOIN [dbo].[ListItem] permitType ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
    WHERE permitApplication.[Id]=@PermitApplicationId;

    SELECT item.[ListItemId],item.[SystemName],item.[ItemName] [Name],item.[Description],item.[DisplayOrder],
           CONVERT(bit,CASE WHEN selection.[PermitApplicationId] IS NULL THEN 0 ELSE selection.[IsSelected] END) [IsSelected]
    FROM [dbo].[ListItem] item
    INNER JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
    LEFT JOIN [dbo].[PermitApplicationInspectionPriorToComm] selection
        ON selection.[PermitApplicationId]=@PermitApplicationId
       AND selection.[InspectionPriorToCommListItemId]=item.[ListItemId]
    WHERE category.[Code]=N''INSPECTIONPRIORTOCOMMENCEMENT''
      AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
    ORDER BY item.[DisplayOrder],item.[ListItemId];

    SELECT item.[ListItemId],item.[SystemName],item.[ItemName] [Name],item.[Description],item.[DisplayOrder],
           CONVERT(bit,CASE WHEN selection.[PermitApplicationId] IS NULL THEN 0 ELSE selection.[IsSelected] END) [IsSelected]
    FROM [dbo].[ListItem] item
    INNER JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
    LEFT JOIN [dbo].[PermitApplicationWallWorks] selection
        ON selection.[PermitApplicationId]=@PermitApplicationId
       AND selection.[WorksonWallListItemId]=item.[ListItemId]
    WHERE category.[Code]=N''WORKSONWALL'' AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
    ORDER BY item.[DisplayOrder],item.[ListItemId];

    SELECT item.[ListItemId],item.[SystemName],item.[ItemName] [Name],item.[Description],item.[DisplayOrder],
           CONVERT(bit,CASE WHEN selection.[PermitApplicationId] IS NULL THEN 0 ELSE selection.[IsSelected] END) [IsSelected]
    FROM [dbo].[ListItem] item
    INNER JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
    LEFT JOIN [dbo].[PermitApplicationConfinedSpace] selection
        ON selection.[PermitApplicationId]=@PermitApplicationId
       AND selection.[WorkingInConfinedSpaceListItemId]=item.[ListItemId]
    WHERE category.[Code]=N''WORNING_IN_CONFINES_SPACE''
      AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
    ORDER BY item.[DisplayOrder],item.[ListItemId];
END;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260910185312_FixPermitApplicationEditorBehavior', N'10.0.9');

COMMIT;
GO

