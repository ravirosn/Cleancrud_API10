using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820100000_AddPermitApplicationsGetProcedure")]
public partial class AddPermitApplicationsGetProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpPermitApplicationsGet]
                @CreatedByUserId int,
                @PageNumber int = 1,
                @PageSize int = 10,
                @SearchTerm nvarchar(200) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @CreatedByUserId < 1
                    THROW 50012, 'CreatedByUserId must be greater than zero.', 1;

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
                FROM [dbo].[PermitApplication] AS permitApplication
                INNER JOIN [dbo].[ListItem] AS permitType
                    ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
                INNER JOIN [dbo].[ListItem] AS permitStatus
                    ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
                INNER JOIN [dbo].[Users] AS createdByUser
                    ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
                WHERE permitApplication.[CreatedByUserId] = @CreatedByUserId
                  AND (@SearchPattern IS NULL
                    OR permitApplication.[PermitNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitIssuerName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PermitReceiverName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitApplication.[PreRiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
                    OR permitType.[ItemName] LIKE @SearchPattern ESCAPE N'\'
                    OR permitStatus.[ItemName] LIKE @SearchPattern ESCAPE N'\');

                SELECT
                    permitApplication.[Id],
                    permitApplication.[PermitNumber],
                    permitApplication.[IssueDate],
                    permitApplication.[PermitIssuerName],
                    permitApplication.[PermitReceiverName],
                    permitApplication.[PermitTypeListItemId],
                    permitType.[ItemName] AS [PermitTypeName],
                    permitApplication.[PermitStatusListItemId],
                    permitStatus.[ItemName] AS [PermitStatusName],
                    permitApplication.[SubmittedAtUtc],
                    permitApplication.[CreatedByUserId],
                    COALESCE(createdByUser.[DisplayName], createdByUser.[UserName]) AS [CreatedByUserName],
                    permitApplication.[PreRiskAssessmentNumber],
                    permitApplication.[RiskAssessmentId]
                FROM [dbo].[PermitApplication] AS permitApplication
                INNER JOIN [dbo].[ListItem] AS permitType
                    ON permitType.[ListItemId] = permitApplication.[PermitTypeListItemId]
                INNER JOIN [dbo].[ListItem] AS permitStatus
                    ON permitStatus.[ListItemId] = permitApplication.[PermitStatusListItemId]
                INNER JOIN [dbo].[Users] AS createdByUser
                    ON createdByUser.[Id] = permitApplication.[CreatedByUserId]
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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpPermitApplicationsGet];");
    }
}
