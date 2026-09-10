namespace Apcloudpms.Infrastructure.Migrations.Sql;

internal static class PermitApplicationPlanningProcedureSql
{
    internal static string CreateUpdateProcedure() => AsExecutableBatch("""
        CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationUpd]
            @PermitApplicationId bigint,
            @IssueDate date,
            @PlannedStartDateTime datetime2(0)=NULL,
            @PlannedEndDateTime datetime2(0)=NULL,
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

            IF @CurrentStatusCode IS NULL THROW 50001, 'Permit application was not found.', 1;
            IF @CurrentStatusCode NOT IN (N'PERMIT_DRAFT',N'PERMIT_REJECTED',N'FINALIZED_FOR_APPROVAL')
                THROW 50002, 'Only Draft, Rejected, or Finalized For Approval permit applications can be edited.', 1;
            IF @PlannedStartDateTime IS NOT NULL AND @PlannedEndDateTime IS NOT NULL
               AND @PlannedEndDateTime < @PlannedStartDateTime
                THROW 50007, 'Planned end date/time must be on or after planned start date/time.', 1;
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id]=@PermitIssuerId AND [IsActive]=1)
               OR NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id]=@PermitReceiverId AND [IsActive]=1)
                THROW 50005, 'Issuer and receiver must be active users.', 1;
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
                WHERE category.[Code]=N'PERMIT_STATUS'
                  AND item.[SystemName]=N'FINALIZED_FOR_APPROVAL' AND item.[IsVisible]=1;
                IF @FinalStatusId IS NULL
                    THROW 50006, 'The active FINALIZED_FOR_APPROVAL permit status is not configured.', 1;
            END;

            UPDATE [dbo].[PermitApplication]
            SET [IssueDate]=@IssueDate,
                [PlannedStartDateTime]=@PlannedStartDateTime,
                [PlannedEndDateTime]=@PlannedEndDateTime,
                [PermitIssuerId]=@PermitIssuerId,
                [PermitIssuerContactNumber]=NULLIF(LTRIM(RTRIM(@PermitIssuerContactNumber)),N''),
                [PermitReceiverId]=@PermitReceiverId,
                [PermitReceiverContactNumber]=NULLIF(LTRIM(RTRIM(@PermitReceiverContactNumber)),N''),
                [WorkLocation]=LTRIM(RTRIM(@WorkLocation)),
                [WorkDescription]=LTRIM(RTRIM(@WorkDescription)),
                [SpecialInstructions]=NULLIF(LTRIM(RTRIM(@SpecialInstructions)),N''),
                [WorkHeightBelowSurface]=NULLIF(LTRIM(RTRIM(@WorkHeightBelowSurface)),N''),
                [CompletionOfWorks]=NULLIF(LTRIM(RTRIM(@CompletionOfWorks)),N''),
                [PermitStatusListItemId]=@FinalStatusId,
                [UpdatedByUserId]=@UpdatedByUserId,
                [UpdatedAtUtc]=@Now
            WHERE [Id]=@PermitApplicationId;

            INSERT [dbo].[AuditLog]
                ([EntityName],[Action],[EntityKey],[ChangedColumns],[ChangedByUserId],[ChangedAtUtc])
            VALUES
                (N'PermitApplication',N'UPDATE',CONCAT(N'{"Id":',@PermitApplicationId,N'}'),
                 N'IssueDate,PlannedStartDateTime,PlannedEndDateTime,PermitIssuerId,PermitReceiverId,WorkLocation,WorkDescription,PermitTypeExtensions',
                 @UpdatedByUserId,@Now);

            SELECT @PermitApplicationId [PermitApplicationId], @FinalStatusId [PermitStatusListItemId],
                   @FinalStatusCode [PermitStatusSystemName], @PermitTypeSystemName [PermitTypeSystemName],
                   @Now [UpdatedAtUtc];
        END;
        """);

    internal static string CreatePreviewProcedure() => AsExecutableBatch("""
        CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationPreviewGet]
            @PermitApplicationId bigint
        AS
        BEGIN
            SET NOCOUNT ON;

            SELECT permitApplication.[Id],permitApplication.[RiskAssessmentId],permitApplication.[PermitNumber],
                   permitApplication.[IssueDate],permitApplication.[PlannedStartDateTime],
                   permitApplication.[PlannedEndDateTime],permitApplication.[PermitIssuerId],
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
            WHERE category.[Code]=N'INSPECTIONPRIORTOCOMMENCEMENT'
              AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
            ORDER BY item.[DisplayOrder],item.[ListItemId];

            SELECT item.[ListItemId],item.[SystemName],item.[ItemName] [Name],item.[Description],item.[DisplayOrder],
                   CONVERT(bit,CASE WHEN selection.[PermitApplicationId] IS NULL THEN 0 ELSE selection.[IsSelected] END) [IsSelected]
            FROM [dbo].[ListItem] item
            INNER JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
            LEFT JOIN [dbo].[PermitApplicationWallWorks] selection
                ON selection.[PermitApplicationId]=@PermitApplicationId
               AND selection.[WorksonWallListItemId]=item.[ListItemId]
            WHERE category.[Code]=N'WORKSONWALL' AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
            ORDER BY item.[DisplayOrder],item.[ListItemId];

            SELECT item.[ListItemId],item.[SystemName],item.[ItemName] [Name],item.[Description],item.[DisplayOrder],
                   CONVERT(bit,CASE WHEN selection.[PermitApplicationId] IS NULL THEN 0 ELSE selection.[IsSelected] END) [IsSelected]
            FROM [dbo].[ListItem] item
            INNER JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
            LEFT JOIN [dbo].[PermitApplicationConfinedSpace] selection
                ON selection.[PermitApplicationId]=@PermitApplicationId
               AND selection.[WorkingInConfinedSpaceListItemId]=item.[ListItemId]
            WHERE category.[Code]=N'WORNING_IN_CONFINES_SPACE'
              AND (item.[IsVisible]=1 OR selection.[PermitApplicationId] IS NOT NULL)
            ORDER BY item.[DisplayOrder],item.[ListItemId];
        END;
        """);

    internal static string RewriteRiskAssessmentProcedures(bool includePlanningDates) => includePlanningDates
        ? RewriteRiskAssessmentProceduresSql(
            "[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],",
            "[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],[PlannedStartDateTime],[PlannedEndDateTime],",
            "@LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,[ListItemId]",
            "@LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,@PlannedStartDateTime,@PlannedEndDateTime,[ListItemId]",
            "[WorkDescription]=COALESCE(@DescriptionOfWork,N''),[SpecialInstructions]=@SpecialInstructions,",
            "[WorkDescription]=COALESCE(@DescriptionOfWork,N''),[SpecialInstructions]=@SpecialInstructions,[PlannedStartDateTime]=@PlannedStartDateTime,[PlannedEndDateTime]=@PlannedEndDateTime,")
        : RewriteRiskAssessmentProceduresSql(
            "[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],[PlannedStartDateTime],[PlannedEndDateTime],",
            "[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],",
            "@LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,@PlannedStartDateTime,@PlannedEndDateTime,[ListItemId]",
            "@LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,[ListItemId]",
            "[WorkDescription]=COALESCE(@DescriptionOfWork,N''),[SpecialInstructions]=@SpecialInstructions,[PlannedStartDateTime]=@PlannedStartDateTime,[PlannedEndDateTime]=@PlannedEndDateTime,",
            "[WorkDescription]=COALESCE(@DescriptionOfWork,N''),[SpecialInstructions]=@SpecialInstructions,");

    private static string RewriteRiskAssessmentProceduresSql(
        string oldColumns, string newColumns, string oldValues, string newValues,
        string oldUpdate, string newUpdate) => $$"""
        DECLARE @ProcedureName sysname, @Definition nvarchar(max), @OriginalDefinition nvarchar(max), @ProcedureKeywordPosition int;
        DECLARE procedure_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT [name] FROM sys.procedures WHERE [name] IN (N'SpRiskAssessmentIns', N'SpRiskAssessmentUpd');
        OPEN procedure_cursor;
        FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @Definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @ProcedureName));
            SET @OriginalDefinition = @Definition;
            SET @Definition = REPLACE(@Definition, N'{{Escape(oldColumns)}}', N'{{Escape(newColumns)}}');
            SET @Definition = REPLACE(@Definition, N'{{Escape(oldValues)}}', N'{{Escape(newValues)}}');
            SET @Definition = REPLACE(@Definition, N'{{Escape(oldUpdate)}}', N'{{Escape(newUpdate)}}');
            IF @Definition = @OriginalDefinition
                THROW 50023, 'A risk-assessment stored procedure did not contain the expected permit planning-date statements.', 1;
            SET @ProcedureKeywordPosition = CHARINDEX(N'PROCEDURE', UPPER(@Definition));
            IF @ProcedureKeywordPosition = 0
                THROW 50022, 'A risk-assessment stored procedure could not be rewritten safely.', 1;
            SET @Definition = N'ALTER PROCEDURE' + SUBSTRING(
                @Definition, @ProcedureKeywordPosition + LEN(N'PROCEDURE'), LEN(@Definition));
            EXEC sys.sp_executesql @Definition;
            FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
        END;
        CLOSE procedure_cursor;
        DEALLOCATE procedure_cursor;
        """;

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string AsExecutableBatch(string definition) =>
        "EXEC(N'" + definition.Replace("'", "''", StringComparison.Ordinal) + "');";
}
