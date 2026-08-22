using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820090000_AddRiskAssessmentGetProcedure")]
public partial class AddRiskAssessmentGetProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentGet]
                @PageNumber int = 1,
                @PageSize int = 10,
                @SearchTerm nvarchar(200) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @PageNumber < 1
                    THROW 50010, 'PageNumber must be greater than zero.', 1;

                IF @PageSize < 1 OR @PageSize > 100
                    THROW 50011, 'PageSize must be between 1 and 100.', 1;

                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

                DECLARE @SearchPattern nvarchar(402) = NULL;
                IF @SearchTerm IS NOT NULL
                    SET @SearchPattern = N'%' +
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(@SearchTerm, N'\', N'\\'),
                                    N'%', N'\%'),
                                N'_', N'\_'),
                            N'[', N'\[') + N'%';

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

                SELECT
                    riskAssessment.[Id],
                    riskAssessment.[PreRiskAssessmentNumber],
                    riskAssessment.[IssueDate],
                    riskAssessment.[PermitIssuerName],
                    riskAssessment.[PermitReceiverName],
                    riskAssessment.[AreaResponsibleName],
                    riskAssessment.[PlannedStartDateTime],
                    riskAssessment.[PlannedEndDateTime],
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
                ORDER BY riskAssessment.[IssueDate] DESC, riskAssessment.[Id] DESC
                OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentGet];");
    }
}
