using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829110000_AddPermitApplicationSorting")]
public partial class AddPermitApplicationSorting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CreateProcedure(includeSorting: true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CreateProcedure(includeSorting: false));
    }

    private static string CreateProcedure(bool includeSorting) => includeSorting
        ? """
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

              IF @SortBy NOT IN (N'preRiskAssessmentNumber', N'permitNumber', N'permitIssuerName',
                  N'permitReceiverName', N'permitTypeName', N'permitStatusName', N'submittedAtUtc', N'issueDate')
                  THROW 50013, 'SortBy is not supported.', 1;
              IF @SortDirection NOT IN ('asc', 'desc')
                  THROW 50014, 'SortDirection must be asc or desc.', 1;

              DECLARE @SearchPattern nvarchar(402) = NULL;
              IF @SearchTerm IS NOT NULL
                  SET @SearchPattern = N'%' +
                      REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

              SELECT COUNT_BIG(1) AS [TotalRecords]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

              SELECT
                  permitApplication.[Id], permitApplication.[PermitNumber], permitApplication.[IssueDate],
                  permitApplication.[PermitIssuerName], permitApplication.[PermitReceiverName],
                  permitApplication.[PermitTypeListItemId], permitType.[ItemName] AS [PermitTypeName],
                  permitApplication.[PermitStatusListItemId], permitStatus.[ItemName] AS [PermitStatusName],
                  permitApplication.[SubmittedAtUtc], permitApplication.[CreatedByUserId],
                  COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
                  permitApplication.[PreRiskAssessmentNumber], permitApplication.[RiskAssessmentId]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\')
              ORDER BY
                  CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'asc' THEN permitApplication.[PreRiskAssessmentNumber] END ASC,
                  CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'desc' THEN permitApplication.[PreRiskAssessmentNumber] END DESC,
                  CASE WHEN @SortBy = N'permitNumber' AND @SortDirection = 'asc' THEN permitApplication.[PermitNumber] END ASC,
                  CASE WHEN @SortBy = N'permitNumber' AND @SortDirection = 'desc' THEN permitApplication.[PermitNumber] END DESC,
                  CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'asc' THEN permitApplication.[PermitIssuerName] END ASC,
                  CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'desc' THEN permitApplication.[PermitIssuerName] END DESC,
                  CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'asc' THEN permitApplication.[PermitReceiverName] END ASC,
                  CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'desc' THEN permitApplication.[PermitReceiverName] END DESC,
                  CASE WHEN @SortBy = N'permitTypeName' AND @SortDirection = 'asc' THEN permitType.[ItemName] END ASC,
                  CASE WHEN @SortBy = N'permitTypeName' AND @SortDirection = 'desc' THEN permitType.[ItemName] END DESC,
                  CASE WHEN @SortBy = N'permitStatusName' AND @SortDirection = 'asc' THEN permitStatus.[ItemName] END ASC,
                  CASE WHEN @SortBy = N'permitStatusName' AND @SortDirection = 'desc' THEN permitStatus.[ItemName] END DESC,
                  CASE WHEN @SortBy = N'submittedAtUtc' AND @SortDirection = 'asc' THEN permitApplication.[SubmittedAtUtc] END ASC,
                  CASE WHEN @SortBy = N'submittedAtUtc' AND @SortDirection = 'desc' THEN permitApplication.[SubmittedAtUtc] END DESC,
                  CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'asc' THEN permitApplication.[IssueDate] END ASC,
                  CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'desc' THEN permitApplication.[IssueDate] END DESC,
                  permitApplication.[Id] DESC
              OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
              FETCH NEXT @PageSize ROWS ONLY;
          END;
          """
        : """
          CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
              @CreatedByUserId int,
              @PageNumber int = 1,
              @PageSize int = 10,
              @SearchTerm nvarchar(200) = NULL
          AS
          BEGIN
              SET NOCOUNT ON;
              IF @CreatedByUserId < 1 THROW 50012, 'CreatedByUserId must be greater than zero.', 1;
              IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
              IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
              SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
              DECLARE @SearchPattern nvarchar(402) = NULL;
              IF @SearchTerm IS NOT NULL
                  SET @SearchPattern = N'%' +
                      REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

              SELECT COUNT_BIG(1) AS [TotalRecords]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

              SELECT permitApplication.[Id], permitApplication.[PermitNumber], permitApplication.[IssueDate],
                  permitApplication.[PermitIssuerName], permitApplication.[PermitReceiverName],
                  permitApplication.[PermitTypeListItemId], permitType.[ItemName] AS [PermitTypeName],
                  permitApplication.[PermitStatusListItemId], permitStatus.[ItemName] AS [PermitStatusName],
                  permitApplication.[SubmittedAtUtc], permitApplication.[CreatedByUserId],
                  COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
                  permitApplication.[PreRiskAssessmentNumber], permitApplication.[RiskAssessmentId]
              FROM [dbo].[PermitApplication] AS permitApplication
              INNER JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
              INNER JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
              INNER JOIN [dbo].[Users] AS createdByUser ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
              WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                AND (@SearchPattern IS NULL
                  OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                  OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                  OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\')
              ORDER BY permitApplication.[IssueDate] DESC, permitApplication.[Id] DESC
              OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
              FETCH NEXT @PageSize ROWS ONLY;
          END;
          """;
}
