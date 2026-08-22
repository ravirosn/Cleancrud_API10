using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822100000_AddPermitApprovalHistoryGetProcedure")]
public partial class AddPermitApprovalHistoryGetProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpPermitApprovalHistoryGet]
                @ActionedByUserId int,
                @ApprovalStatus varchar(20),
                @PageNumber int = 1,
                @PageSize int = 10,
                @SearchTerm nvarchar(200) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @ActionedByUserId < 1
                    THROW 50012, 'ActionedByUserId must be greater than zero.', 1;

                IF @ApprovalStatus NOT IN ('APPROVED', 'REJECTED')
                    THROW 50013, 'ApprovalStatus must be APPROVED or REJECTED.', 1;

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
                FROM [dbo].[PermitApproval] AS permitApproval
                INNER JOIN [dbo].[PermitApplication] AS permitApplication
                    ON permitApplication.[Id] = permitApproval.[PermitApplicationId]
                INNER JOIN [dbo].[ListItem] AS permitType
                    ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
                INNER JOIN [dbo].[ListItem] AS permitStatus
                    ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
                WHERE permitApproval.[ActionedByUserId] = @ActionedByUserId
                  AND permitApproval.[Status] = @ApprovalStatus
                  AND (@SearchPattern IS NULL
                    OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\');

                SELECT
                    permitApplication.[PreRiskAssessmentNumber],
                    permitApplication.[PermitNumber],
                    permitApplication.[IssueDate] AS [IssuedDate],
                    permitApplication.[PermitIssuerName],
                    permitApplication.[PermitReceiverName],
                    permitType.[ItemName] AS [PermitType],
                    permitStatus.[ItemName] AS [PermitStatus],
                    permitApproval.[ActionedAtUtc] AS [DecisionDate],
                    permitApproval.[Comments] AS [Remarks]
                FROM [dbo].[PermitApproval] AS permitApproval
                INNER JOIN [dbo].[PermitApplication] AS permitApplication
                    ON permitApplication.[Id] = permitApproval.[PermitApplicationId]
                INNER JOIN [dbo].[ListItem] AS permitType
                    ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
                INNER JOIN [dbo].[ListItem] AS permitStatus
                    ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
                WHERE permitApproval.[ActionedByUserId] = @ActionedByUserId
                  AND permitApproval.[Status] = @ApprovalStatus
                  AND (@SearchPattern IS NULL
                    OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApproval.[Comments] LIKE @SearchPattern ESCAPE N'\')
                ORDER BY permitApproval.[ActionedAtUtc] DESC, permitApproval.[Id] DESC
                OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP PROCEDURE IF EXISTS [dbo].[SpPermitApprovalHistoryGet];");
    }
}
