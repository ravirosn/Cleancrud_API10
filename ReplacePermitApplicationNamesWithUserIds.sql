BEGIN TRANSACTION;
ALTER TABLE [dbo].[PermitApplication] ADD [PermitIssuerId] int NULL;

ALTER TABLE [dbo].[PermitApplication] ADD [PermitReceiverId] int NULL;

UPDATE permitApplication
SET [PermitIssuerId] = COALESCE(riskAssessment.[PermitIssuerUserId], issuerMatch.[UserId]),
    [PermitReceiverId] = COALESCE(riskAssessment.[PermitReceiverUserId], receiverMatch.[UserId])
FROM [dbo].[PermitApplication] permitApplication
LEFT JOIN [dbo].[RiskAssessment] riskAssessment ON riskAssessment.[Id] = permitApplication.[RiskAssessmentId]
OUTER APPLY
(
    SELECT MIN([Id]) AS [UserId], COUNT_BIG(*) AS [MatchCount]
    FROM [dbo].[Users]
    WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
       OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
) issuerMatch
OUTER APPLY
(
    SELECT MIN([Id]) AS [UserId], COUNT_BIG(*) AS [MatchCount]
    FROM [dbo].[Users]
    WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
       OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
) receiverMatch;

IF EXISTS
(
    SELECT 1
    FROM [dbo].[PermitApplication] permitApplication
    LEFT JOIN [dbo].[RiskAssessment] riskAssessment ON riskAssessment.[Id] = permitApplication.[RiskAssessmentId]
    OUTER APPLY
    (
        SELECT COUNT_BIG(*) AS [MatchCount] FROM [dbo].[Users]
        WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
           OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
    ) issuerMatch
    OUTER APPLY
    (
        SELECT COUNT_BIG(*) AS [MatchCount] FROM [dbo].[Users]
        WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
           OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
    ) receiverMatch
    WHERE permitApplication.[PermitIssuerId] IS NULL
       OR permitApplication.[PermitReceiverId] IS NULL
       OR (riskAssessment.[Id] IS NULL AND (issuerMatch.[MatchCount] <> 1 OR receiverMatch.[MatchCount] <> 1))
)
    THROW 50021, 'PermitApplication issuer/receiver names could not be mapped uniquely to users.', 1;

ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitIssuerId] int NOT NULL;

ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitReceiverId] int NOT NULL;

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

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[PermitApplication]') AND [c].[name] = N'PermitIssuerName');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[PermitApplication] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [dbo].[PermitApplication] DROP COLUMN [PermitIssuerName];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[PermitApplication]') AND [c].[name] = N'PermitReceiverName');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[PermitApplication] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [dbo].[PermitApplication] DROP COLUMN [PermitReceiverName];

CREATE INDEX [IX_PermitApplication_PermitIssuerId] ON [dbo].[PermitApplication] ([PermitIssuerId]);

CREATE INDEX [IX_PermitApplication_PermitReceiverId] ON [dbo].[PermitApplication] ([PermitReceiverId]);

ALTER TABLE [dbo].[PermitApplication] ADD CONSTRAINT [FK_PermitApplication_Users_PermitIssuerId] FOREIGN KEY ([PermitIssuerId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [dbo].[PermitApplication] ADD CONSTRAINT [FK_PermitApplication_Users_PermitReceiverId] FOREIGN KEY ([PermitReceiverId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
    @CreatedByUserId int,
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @SortBy nvarchar(40) = N''issueDate'',
    @SortDirection varchar(4) = ''desc''
AS
BEGIN
    SET NOCOUNT ON;
    IF @CreatedByUserId < 1 THROW 50012, ''CreatedByUserId must be greater than zero.'', 1;
    IF @PageNumber < 1 THROW 50010, ''PageNumber must be greater than zero.'', 1;
    IF @PageSize < 1 OR @PageSize > 100 THROW 50011, ''PageSize must be between 1 and 100.'', 1;
    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'''');
    SET @SortBy = NULLIF(LTRIM(RTRIM(@SortBy)), N'''');
    SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''''));
    IF @SortBy NOT IN (N''riskAssessmentNumber'', N''permitNumber'', N''permitIssuerName'',
        N''permitReceiverName'', N''permitTypeName'', N''permitStatusName'', N''submittedAtUtc'', N''issueDate'')
        THROW 50013, ''SortBy is not supported.'', 1;
    IF @SortDirection NOT IN (''asc'', ''desc'') THROW 50014, ''SortDirection must be asc or desc.'', 1;

    DECLARE @SearchPattern nvarchar(402) = NULL;
    IF @SearchTerm IS NOT NULL
        SET @SearchPattern = N''%'' + REPLACE(REPLACE(REPLACE(REPLACE(
            @SearchTerm, N''\'', N''\\''), N''%'', N''\%''), N''_'', N''\_''), N''['', N''\['') + N''%'';

    SELECT COUNT_BIG(1) AS [TotalRecords]
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    INNER JOIN [dbo].[Users] AS issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
INNER JOIN [dbo].[Users] AS receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N''\''
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N''\''
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N''\'');

    SELECT permitApplication.[Id], permitApplication.[PermitNumber], permitApplication.[IssueDate],
        permitApplication.[PermitIssuerId], COALESCE(issuer.[DisplayName], issuer.[UserName]) AS [PermitIssuerName], permitApplication.[PermitReceiverId], COALESCE(receiver.[DisplayName], receiver.[UserName]) AS [PermitReceiverName],
        permitApplication.[PermitTypeListItemId], permitType.[ItemName] AS [PermitTypeName],
        permitApplication.[PermitStatusListItemId], permitStatus.[ItemName] AS [PermitStatusName],
        permitApplication.[SubmittedAtUtc], permitApplication.[CreatedByUserId],
        COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
        permitApplication.[RiskAssessmentNumber], permitApplication.[RiskAssessmentId]
    FROM [dbo].[PermitApplication] AS permitApplication
    INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
    INNER JOIN [dbo].[Users] AS issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
INNER JOIN [dbo].[Users] AS receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
    WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
      AND (@SearchPattern IS NULL
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N''\''
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N''\''
        OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N''\'')
    ORDER BY
        CASE WHEN @SortBy=N''riskAssessmentNumber'' AND @SortDirection=''asc'' THEN permitApplication.[RiskAssessmentNumber] END ASC,
        CASE WHEN @SortBy=N''riskAssessmentNumber'' AND @SortDirection=''desc'' THEN permitApplication.[RiskAssessmentNumber] END DESC,
        CASE WHEN @SortBy=N''permitNumber'' AND @SortDirection=''asc'' THEN permitApplication.[PermitNumber] END ASC,
        CASE WHEN @SortBy=N''permitNumber'' AND @SortDirection=''desc'' THEN permitApplication.[PermitNumber] END DESC,
        CASE WHEN @SortBy=N''permitIssuerName'' AND @SortDirection=''asc'' THEN COALESCE(issuer.[DisplayName], issuer.[UserName]) END ASC,
        CASE WHEN @SortBy=N''permitIssuerName'' AND @SortDirection=''desc'' THEN COALESCE(issuer.[DisplayName], issuer.[UserName]) END DESC,
        CASE WHEN @SortBy=N''permitReceiverName'' AND @SortDirection=''asc'' THEN COALESCE(receiver.[DisplayName], receiver.[UserName]) END ASC,
        CASE WHEN @SortBy=N''permitReceiverName'' AND @SortDirection=''desc'' THEN COALESCE(receiver.[DisplayName], receiver.[UserName]) END DESC,
        CASE WHEN @SortBy=N''permitTypeName'' AND @SortDirection=''asc'' THEN permitType.[ItemName] END ASC,
        CASE WHEN @SortBy=N''permitTypeName'' AND @SortDirection=''desc'' THEN permitType.[ItemName] END DESC,
        CASE WHEN @SortBy=N''permitStatusName'' AND @SortDirection=''asc'' THEN permitStatus.[ItemName] END ASC,
        CASE WHEN @SortBy=N''permitStatusName'' AND @SortDirection=''desc'' THEN permitStatus.[ItemName] END DESC,
        CASE WHEN @SortBy=N''submittedAtUtc'' AND @SortDirection=''asc'' THEN permitApplication.[SubmittedAtUtc] END ASC,
        CASE WHEN @SortBy=N''submittedAtUtc'' AND @SortDirection=''desc'' THEN permitApplication.[SubmittedAtUtc] END DESC,
        CASE WHEN @SortBy=N''issueDate'' AND @SortDirection=''asc'' THEN permitApplication.[IssueDate] END ASC,
        CASE WHEN @SortBy=N''issueDate'' AND @SortDirection=''desc'' THEN permitApplication.[IssueDate] END DESC,
        permitApplication.[Id] DESC
    OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;');

EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[SpPermitApprovalHistoryGet]
    @ActionedByUserId int, @ApprovalStatus varchar(20), @PageNumber int=1,
    @PageSize int=10, @SearchTerm nvarchar(200)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @ActionedByUserId<1 THROW 50012, ''ActionedByUserId must be greater than zero.'', 1;
    IF @ApprovalStatus NOT IN (''APPROVED'',''REJECTED'') THROW 50013, ''ApprovalStatus must be APPROVED or REJECTED.'', 1;
    IF @PageNumber<1 THROW 50010, ''PageNumber must be greater than zero.'', 1;
    IF @PageSize<1 OR @PageSize>100 THROW 50011, ''PageSize must be between 1 and 100.'', 1;
    SET @SearchTerm=NULLIF(LTRIM(RTRIM(@SearchTerm)),N'''');
    DECLARE @SearchPattern nvarchar(402)=NULL;
    IF @SearchTerm IS NOT NULL SET @SearchPattern=N''%''+REPLACE(REPLACE(REPLACE(REPLACE(
        @SearchTerm,N''\'',N''\\''),N''%'',N''\%''),N''_'',N''\_''),N''['',N''\['')+N''%'';

    SELECT COUNT_BIG(1) [TotalRecords]
    FROM [dbo].[PermitApproval] permitApproval
    INNER JOIN [dbo].[PermitApplication] permitApplication ON permitApplication.[Id]=permitApproval.[PermitApplicationId]
    INNER JOIN [dbo].[ListItem] permitType ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
    WHERE permitApproval.[ActionedByUserId]=@ActionedByUserId AND permitApproval.[Status]=@ApprovalStatus
      AND (@SearchPattern IS NULL OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N''\''
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N''\'' OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N''\'' OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N''\''
        OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N''\'');

    SELECT permitApplication.[RiskAssessmentNumber], permitApplication.[PermitNumber],
        permitApplication.[IssueDate] [IssuedDate], COALESCE(issuer.[DisplayName], issuer.[UserName]) [PermitIssuerName],
        COALESCE(receiver.[DisplayName], receiver.[UserName]) [PermitReceiverName], permitType.[ItemName] [PermitType],
        permitStatus.[ItemName] [PermitStatus], permitApproval.[ActionedAtUtc] [DecisionDate],
        permitApproval.[Comments] [Remarks]
    FROM [dbo].[PermitApproval] permitApproval
    INNER JOIN [dbo].[PermitApplication] permitApplication ON permitApplication.[Id]=permitApproval.[PermitApplicationId]
    INNER JOIN [dbo].[ListItem] permitType ON permitType.[ListItemId]=permitApplication.[PermitTypeListItemId]
    INNER JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
    INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = permitApplication.[PermitReceiverId]
    WHERE permitApproval.[ActionedByUserId]=@ActionedByUserId AND permitApproval.[Status]=@ApprovalStatus
      AND (@SearchPattern IS NULL OR permitApplication.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N''\''
        OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N''\''
        OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N''\'' OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N''\''
        OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N''\'' OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N''\''
        OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N''\'')
    ORDER BY permitApproval.[ActionedAtUtc] DESC,permitApproval.[Id] DESC
    OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260910163925_ReplacePermitApplicationNamesWithUserIds', N'10.0.9');

COMMIT;
GO

