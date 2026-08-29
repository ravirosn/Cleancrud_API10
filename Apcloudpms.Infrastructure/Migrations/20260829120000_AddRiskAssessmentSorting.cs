using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260829120000_AddRiskAssessmentSorting")]
public partial class AddRiskAssessmentSorting : Migration
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
          CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentGet]
              @PageNumber int = 1,
              @PageSize int = 10,
              @SearchTerm nvarchar(200) = NULL,
              @SortBy nvarchar(40) = N'issueDate',
              @SortDirection varchar(4) = 'desc'
          AS
          BEGIN
              SET NOCOUNT ON;
              IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
              IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;

              SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
              SET @SortBy = NULLIF(LTRIM(RTRIM(@SortBy)), N'');
              SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), ''));
              IF @SortBy NOT IN (N'preRiskAssessmentNumber', N'issueDate', N'permitIssuerName',
                  N'permitReceiverName', N'areaResponsibleName', N'plannedStartDateTime',
                  N'plannedEndDateTime', N'riskAssessmentStatus')
                  THROW 50013, 'SortBy is not supported.', 1;
              IF @SortDirection NOT IN ('asc', 'desc')
                  THROW 50014, 'SortDirection must be asc or desc.', 1;

              DECLARE @SearchPattern nvarchar(402) = NULL;
              IF @SearchTerm IS NOT NULL
                  SET @SearchPattern = N'%' +
                      REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

              SELECT COUNT_BIG(1) AS [TotalRecords]
              FROM [dbo].[RiskAssessment] AS riskAssessment
              INNER JOIN [dbo].[ListItem] AS statusItem
                  ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
              WHERE @SearchPattern IS NULL
                 OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
                 OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\';

              SELECT riskAssessment.[Id], riskAssessment.[PreRiskAssessmentNumber],
                  riskAssessment.[IssueDate], riskAssessment.[PermitIssuerName],
                  riskAssessment.[PermitReceiverName], riskAssessment.[AreaResponsibleName],
                  riskAssessment.[PlannedStartDateTime], riskAssessment.[PlannedEndDateTime],
                  riskAssessment.[RiskAssessmentStatusListItemId],
                  statusItem.[ItemName] AS [RiskAssessmentStatus]
              FROM [dbo].[RiskAssessment] AS riskAssessment
              INNER JOIN [dbo].[ListItem] AS statusItem
                  ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
              WHERE @SearchPattern IS NULL
                 OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
                 OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\'
              ORDER BY
                  CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'asc' THEN riskAssessment.[PreRiskAssessmentNumber] END ASC,
                  CASE WHEN @SortBy = N'preRiskAssessmentNumber' AND @SortDirection = 'desc' THEN riskAssessment.[PreRiskAssessmentNumber] END DESC,
                  CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'asc' THEN riskAssessment.[IssueDate] END ASC,
                  CASE WHEN @SortBy = N'issueDate' AND @SortDirection = 'desc' THEN riskAssessment.[IssueDate] END DESC,
                  CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'asc' THEN riskAssessment.[PermitIssuerName] END ASC,
                  CASE WHEN @SortBy = N'permitIssuerName' AND @SortDirection = 'desc' THEN riskAssessment.[PermitIssuerName] END DESC,
                  CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'asc' THEN riskAssessment.[PermitReceiverName] END ASC,
                  CASE WHEN @SortBy = N'permitReceiverName' AND @SortDirection = 'desc' THEN riskAssessment.[PermitReceiverName] END DESC,
                  CASE WHEN @SortBy = N'areaResponsibleName' AND @SortDirection = 'asc' THEN riskAssessment.[AreaResponsibleName] END ASC,
                  CASE WHEN @SortBy = N'areaResponsibleName' AND @SortDirection = 'desc' THEN riskAssessment.[AreaResponsibleName] END DESC,
                  CASE WHEN @SortBy = N'plannedStartDateTime' AND @SortDirection = 'asc' THEN riskAssessment.[PlannedStartDateTime] END ASC,
                  CASE WHEN @SortBy = N'plannedStartDateTime' AND @SortDirection = 'desc' THEN riskAssessment.[PlannedStartDateTime] END DESC,
                  CASE WHEN @SortBy = N'plannedEndDateTime' AND @SortDirection = 'asc' THEN riskAssessment.[PlannedEndDateTime] END ASC,
                  CASE WHEN @SortBy = N'plannedEndDateTime' AND @SortDirection = 'desc' THEN riskAssessment.[PlannedEndDateTime] END DESC,
                  CASE WHEN @SortBy = N'riskAssessmentStatus' AND @SortDirection = 'asc' THEN statusItem.[ItemName] END ASC,
                  CASE WHEN @SortBy = N'riskAssessmentStatus' AND @SortDirection = 'desc' THEN statusItem.[ItemName] END DESC,
                  riskAssessment.[Id] DESC
              OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
              FETCH NEXT @PageSize ROWS ONLY;
          END;
          """
        : """
          CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentGet]
              @PageNumber int = 1,
              @PageSize int = 10,
              @SearchTerm nvarchar(200) = NULL
          AS
          BEGIN
              SET NOCOUNT ON;
              IF @PageNumber < 1 THROW 50010, 'PageNumber must be greater than zero.', 1;
              IF @PageSize < 1 OR @PageSize > 100 THROW 50011, 'PageSize must be between 1 and 100.', 1;
              SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
              DECLARE @SearchPattern nvarchar(402) = NULL;
              IF @SearchTerm IS NOT NULL
                  SET @SearchPattern = N'%' +
                      REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

              SELECT COUNT_BIG(1) AS [TotalRecords]
              FROM [dbo].[RiskAssessment] AS riskAssessment
              INNER JOIN [dbo].[ListItem] AS statusItem ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
              WHERE @SearchPattern IS NULL
                 OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
                 OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\';

              SELECT riskAssessment.[Id], riskAssessment.[PreRiskAssessmentNumber], riskAssessment.[IssueDate],
                  riskAssessment.[PermitIssuerName], riskAssessment.[PermitReceiverName], riskAssessment.[AreaResponsibleName],
                  riskAssessment.[PlannedStartDateTime], riskAssessment.[PlannedEndDateTime],
                  riskAssessment.[RiskAssessmentStatusListItemId], statusItem.[ItemName] AS [RiskAssessmentStatus]
              FROM [dbo].[RiskAssessment] AS riskAssessment
              INNER JOIN [dbo].[ListItem] AS statusItem ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
              WHERE @SearchPattern IS NULL
                 OR riskAssessment.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                 OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
                 OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\'
              ORDER BY riskAssessment.[IssueDate] DESC, riskAssessment.[Id] DESC
              OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
              FETCH NEXT @PageSize ROWS ONLY;
          END;
          """;
}
