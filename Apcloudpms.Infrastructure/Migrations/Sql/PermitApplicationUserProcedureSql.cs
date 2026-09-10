namespace Apcloudpms.Infrastructure.Migrations.Sql;

internal static class PermitApplicationUserProcedureSql
{
    internal static string RewriteRiskAssessmentProcedures(bool useUserIds) => useUserIds
        ? """
          DECLARE @ProcedureName sysname, @Definition nvarchar(max);
          DECLARE procedure_cursor CURSOR LOCAL FAST_FORWARD FOR
              SELECT [name] FROM sys.procedures WHERE [name] IN (N'SpRiskAssessmentIns', N'SpRiskAssessmentUpd');
          OPEN procedure_cursor;
          FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
          WHILE @@FETCH_STATUS = 0
          BEGIN
              SET @Definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @ProcedureName));
              SET @Definition = REPLACE(@Definition, N'[PermitIssuerName]', N'[PermitIssuerId]');
              SET @Definition = REPLACE(@Definition, N'[PermitReceiverName]', N'[PermitReceiverId]');
              SET @Definition = REPLACE(@Definition, N'COALESCE([DisplayName],[UserName])', N'CONVERT(nvarchar(200),[Id])');
              DECLARE @ProcedureKeywordPosition int = CHARINDEX(N'PROCEDURE', UPPER(@Definition));
              IF @ProcedureKeywordPosition = 0
                  THROW 50022, 'A risk-assessment stored procedure could not be rewritten safely.', 1;
              SET @Definition = N'ALTER PROCEDURE' + SUBSTRING(
                  @Definition,
                  @ProcedureKeywordPosition + LEN(N'PROCEDURE'),
                  LEN(@Definition));
              EXEC sys.sp_executesql @Definition;
              FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
          END;
          CLOSE procedure_cursor;
          DEALLOCATE procedure_cursor;
          """
        : """
          DECLARE @ProcedureName sysname, @Definition nvarchar(max);
          DECLARE procedure_cursor CURSOR LOCAL FAST_FORWARD FOR
              SELECT [name] FROM sys.procedures WHERE [name] IN (N'SpRiskAssessmentIns', N'SpRiskAssessmentUpd');
          OPEN procedure_cursor;
          FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
          WHILE @@FETCH_STATUS = 0
          BEGIN
              SET @Definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @ProcedureName));
              SET @Definition = REPLACE(@Definition, N'[PermitIssuerId]', N'[PermitIssuerName]');
              SET @Definition = REPLACE(@Definition, N'[PermitReceiverId]', N'[PermitReceiverName]');
              SET @Definition = REPLACE(@Definition, N'CONVERT(nvarchar(200),[Id])', N'COALESCE([DisplayName],[UserName])');
              DECLARE @ProcedureKeywordPosition int = CHARINDEX(N'PROCEDURE', UPPER(@Definition));
              IF @ProcedureKeywordPosition = 0
                  THROW 50022, 'A risk-assessment stored procedure could not be rewritten safely.', 1;
              SET @Definition = N'ALTER PROCEDURE' + SUBSTRING(
                  @Definition,
                  @ProcedureKeywordPosition + LEN(N'PROCEDURE'),
                  LEN(@Definition));
              EXEC sys.sp_executesql @Definition;
              FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
          END;
          CLOSE procedure_cursor;
          DEALLOCATE procedure_cursor;
          """;

    internal static string CreatePermitApplicationsGet(bool useUserIds)
    {
        var userJoins = useUserIds
            ? """
              INNER JOIN [dbo].[Users] AS issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
              INNER JOIN [dbo].[Users] AS receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
              """
            : string.Empty;
        var issuerName = useUserIds
            ? "COALESCE(issuer.[DisplayName], issuer.[UserName])"
            : "permitApplication.[PermitIssuerName]";
        var receiverName = useUserIds
            ? "COALESCE(receiver.[DisplayName], receiver.[UserName])"
            : "permitApplication.[PermitReceiverName]";
        var identityColumns = useUserIds
            ? "permitApplication.[PermitIssuerId], " + issuerName + " AS [PermitIssuerName], " +
              "permitApplication.[PermitReceiverId], " + receiverName + " AS [PermitReceiverName],"
            : issuerName + " AS [PermitIssuerName], " + receiverName + " AS [PermitReceiverName],";

        var procedure = $$"""
          CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
              @CreatedByUserId int,
              @PageNumber int = 1,
              @PageSize int = 10,
              @SearchTerm nvarchar(200) = NULL,
              @SortBy nvarchar(40) = N'issueDate',
              @SortDirection varchar(4) = 'desc'
          AS
          BEGIN
              SET NOCOUNT ON;
              IF @CreatedByUserId < 1 THROW 50012, 'CreatedByUserId must be greater than zero.', 1;
              IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
              IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
              SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
              SET @SortBy = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
              SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));
              IF @SortBy NOT IN (N'riskAssessmentNumber', N'permitNumber', N'permitIssuerName',
                  N'permitReceiverName', N'permitTypeName', N'permitStatusName', N'submittedAtUtc', N'issueDate')
                  THROW 50013, 'SortBy is not supported.', 1;
              IF @SortDirection NOT IN ('asc', 'desc') THROW 50014, 'SortDirection must be asc or desc.', 1;

              DECLARE @SearchPattern nvarchar(402) = NULL;
              IF @SearchTerm IS NOT NULL
                  SET @SearchPattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(
                      @SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

              SELECT COUNT_BIG(1) AS [TotalRecords]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              {{userJoins}}
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR {{issuerName}} LIKE @SearchPattern ESCAPE N'\'
                  OR {{receiverName}} LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

              SELECT permitApplication.[Id], permitApplication.[PermitNumber], permitApplication.[IssueDate],
                  {{identityColumns}}
                  permitApplication.[PermitTypeListItemId], permitType.[ItemName] AS [PermitTypeName],
                  permitApplication.[PermitStatusListItemId], permitStatus.[ItemName] AS [PermitStatusName],
                  permitApplication.[SubmittedAtUtc], permitApplication.[CreatedByUserId],
                  COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
                  permitApplication.[RiskAssessmentNumber], permitApplication.[RiskAssessmentId]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              {{userJoins}}
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR {{issuerName}} LIKE @SearchPattern ESCAPE N'\'
                  OR {{receiverName}} LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\')
              ORDER BY
                  CASE WHEN @SortBy=N'riskAssessmentNumber' AND @SortDirection='asc' THEN permitApplication.[RiskAssessmentNumber] END ASC,
                  CASE WHEN @SortBy=N'riskAssessmentNumber' AND @SortDirection='desc' THEN permitApplication.[RiskAssessmentNumber] END DESC,
                  CASE WHEN @SortBy=N'permitNumber' AND @SortDirection='asc' THEN permitApplication.[PermitNumber] END ASC,
                  CASE WHEN @SortBy=N'permitNumber' AND @SortDirection='desc' THEN permitApplication.[PermitNumber] END DESC,
                  CASE WHEN @SortBy=N'permitIssuerName' AND @SortDirection='asc' THEN {{issuerName}} END ASC,
                  CASE WHEN @SortBy=N'permitIssuerName' AND @SortDirection='desc' THEN {{issuerName}} END DESC,
                  CASE WHEN @SortBy=N'permitReceiverName' AND @SortDirection='asc' THEN {{receiverName}} END ASC,
                  CASE WHEN @SortBy=N'permitReceiverName' AND @SortDirection='desc' THEN {{receiverName}} END DESC,
                  CASE WHEN @SortBy=N'permitTypeName' AND @SortDirection='asc' THEN permitType.[ItemName] END ASC,
                  CASE WHEN @SortBy=N'permitTypeName' AND @SortDirection='desc' THEN permitType.[ItemName] END DESC,
                  CASE WHEN @SortBy=N'permitStatusName' AND @SortDirection='asc' THEN permitStatus.[ItemName] END ASC,
                  CASE WHEN @SortBy=N'permitStatusName' AND @SortDirection='desc' THEN permitStatus.[ItemName] END DESC,
                  CASE WHEN @SortBy=N'submittedAtUtc' AND @SortDirection='asc' THEN permitApplication.[SubmittedAtUtc] END ASC,
                  CASE WHEN @SortBy=N'submittedAtUtc' AND @SortDirection='desc' THEN permitApplication.[SubmittedAtUtc] END DESC,
                  CASE WHEN @SortBy=N'issueDate' AND @SortDirection='asc' THEN permitApplication.[IssueDate] END ASC,
                  CASE WHEN @SortBy=N'issueDate' AND @SortDirection='desc' THEN permitApplication.[IssueDate] END DESC,
                  permitApplication.[Id] DESC
              OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
          END;
          """;
        return AsExecutableBatch(procedure);
    }

    internal static string CreatePermitApprovalHistoryGet(bool useUserIds)
    {
        var userJoins = useUserIds
            ? """
              INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
              INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
              """
            : string.Empty;
        var issuerName = useUserIds ? "COALESCE(issuer.[DisplayName], issuer.[UserName])" : "permitApplication.[PermitIssuerName]";
        var receiverName = useUserIds ? "COALESCE(receiver.[DisplayName], receiver.[UserName])" : "permitApplication.[PermitReceiverName]";

        var procedure = $$"""
          CREATE OR ALTER PROCEDURE [dbo].[SpPermitApprovalHistoryGet]
              @ActionedByUserId int, @ApprovalStatus varchar(20), @PageNumber int=1,
              @PageSize int=10, @SearchTerm nvarchar(200)=NULL
          AS
          BEGIN
              SET NOCOUNT ON;
              IF @ActionedByUserId<1 THROW 50012, 'ActionedByUserId must be greater than zero.', 1;
              IF @ApprovalStatus NOT IN ('APPROVED','REJECTED') THROW 50013, 'ApprovalStatus must be APPROVED or REJECTED.', 1;
              IF @PageNumber<1 THROW 50010, 'PageNumber must be greater than zero.', 1;
              IF @PageSize<1 OR @PageSize>100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
              SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'');
              DECLARE @SearchPattern nvarchar(402)=NULL;
              IF @SearchTerm IS NOT NULL SET @SearchPattern=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(
                  @SearchTerm,N'\',N'\\'),N'%',N'\%'),N'_',N'\_'),N'[',N'\[')+N'%';

              SELECT COUNT_BIG(1) [TotalRecords]
              FROM [dbo].[PermitApproval] permitApproval
              INNER JOIN [dbo].[PermitApplication] permitApplication ON permitApplication.[Id]=permitApproval.[PermitApplicationId]
              INNER JOIN [dbo].[ListItem] permitType ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
              {{userJoins}}
              WHERE permitApproval.[ActionedByUserId]=@ActionedByUserId AND permitApproval.[Status]=@ApprovalStatus
                AND (@SearchPattern IS NULL OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR {{issuerName}} LIKE @SearchPattern ESCAPE N'\' OR {{receiverName}} LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\' OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\');

              SELECT permitApplication.[RiskAssessmentNumber], permitApplication.[PermitNumber],
                  permitApplication.[IssueDate] [IssuedDate], {{issuerName}} [PermitIssuerName],
                  {{receiverName}} [PermitReceiverName], permitType.[ItemName] [PermitType],
                  permitStatus.[ItemName] [PermitStatus], permitApproval.[ActionedAtUtc] [DecisionDate],
                  permitApproval.[Comments] [Remarks]
              FROM [dbo].[PermitApproval] permitApproval
              INNER JOIN [dbo].[PermitApplication] permitApplication ON permitApplication.[Id]=permitApproval.[PermitApplicationId]
              INNER JOIN [dbo].[ListItem] permitType ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
              {{userJoins}}
              WHERE permitApproval.[ActionedByUserId]=@ActionedByUserId AND permitApproval.[Status]=@ApprovalStatus
                AND (@SearchPattern IS NULL OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR {{issuerName}} LIKE @SearchPattern ESCAPE N'\' OR {{receiverName}} LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\' OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\')
              ORDER BY permitApproval.[ActionedAtUtc] DESC,permitApproval.[Id] DESC
              OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
          END;
          """;
        return AsExecutableBatch(procedure);
    }

    private static string AsExecutableBatch(string procedureDefinition) =>
        "EXEC(N'" + procedureDefinition.Replace("'", "''", StringComparison.Ordinal) + "');";
}
