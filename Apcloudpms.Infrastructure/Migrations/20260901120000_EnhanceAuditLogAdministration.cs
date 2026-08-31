using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901120000_EnhanceAuditLogAdministration")]
public sealed class EnhanceAuditLogAdministration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpAuditLogFilterOptionsGet]
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT DISTINCT [EntityName]
                FROM [dbo].[AuditLog]
                WHERE NULLIF(LTRIM(RTRIM([EntityName])), N'') IS NOT NULL
                ORDER BY [EntityName];

                SELECT DISTINCT [Action]
                FROM [dbo].[AuditLog]
                WHERE NULLIF(LTRIM(RTRIM([Action])), N'') IS NOT NULL
                ORDER BY [Action];
            END;
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpAuditLogsAdminGet]
                @PageNumber int = 1,
                @PageSize int = 20,
                @SearchTerm nvarchar(200) = NULL,
                @EntityName nvarchar(128) = NULL,
                @Action nvarchar(20) = NULL,
                @ChangedBy nvarchar(200) = NULL,
                @FromUtc datetime2 = NULL,
                @ToUtc datetime2 = NULL,
                @SortBy nvarchar(32) = N'changedAtUtc',
                @SortDirection nvarchar(4) = N'desc'
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @PageNumber < 1
                    THROW 50010, 'PageNumber must be greater than zero.', 1;
                IF @PageSize < 1 OR @PageSize > 10000
                    THROW 50011, 'PageSize must be between 1 and 10000.', 1;
                IF @FromUtc IS NOT NULL AND @ToUtc IS NOT NULL AND @FromUtc > @ToUtc
                    THROW 50012, 'FromUtc cannot be later than ToUtc.', 1;

                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
                SET @EntityName = NULLIF(LTRIM(RTRIM(@EntityName)), N'');
                SET @Action = NULLIF(LTRIM(RTRIM(@Action)), N'');
                SET @ChangedBy = NULLIF(LTRIM(RTRIM(@ChangedBy)), N'');
                SET @SortBy = LOWER(NULLIF(LTRIM(RTRIM(@SortBy)), N''));
                SET @SortDirection = LOWER(NULLIF(LTRIM(RTRIM(@SortDirection)), N''));

                IF @SortBy NOT IN (N'changedatutc', N'entityname', N'action', N'changedbyname')
                    SET @SortBy = N'changedatutc';
                IF @SortDirection NOT IN (N'asc', N'desc')
                    SET @SortDirection = N'desc';

                DECLARE @SearchPattern nvarchar(402) = NULL;
                DECLARE @ChangedByPattern nvarchar(402) = NULL;
                IF @SearchTerm IS NOT NULL
                    SET @SearchPattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';
                IF @ChangedBy IS NOT NULL
                    SET @ChangedByPattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(@ChangedBy, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

                SELECT COUNT_BIG(1) AS [TotalRecords]
                FROM [dbo].[AuditLog] auditLog
                LEFT JOIN [dbo].[Users] changedByUser ON changedByUser.[Id] = auditLog.[ChangedByUserId]
                WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
                  AND (@Action IS NULL OR auditLog.[Action] = @Action)
                  AND (@FromUtc IS NULL OR auditLog.[ChangedAtUtc] >= @FromUtc)
                  AND (@ToUtc IS NULL OR auditLog.[ChangedAtUtc] <= @ToUtc)
                  AND (@ChangedByPattern IS NULL OR COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy], N'System') LIKE @ChangedByPattern ESCAPE N'\')
                  AND (@SearchPattern IS NULL
                    OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[EntityKey] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[OldValues] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[NewValues] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[TraceId] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[IpAddress] LIKE @SearchPattern ESCAPE N'\'
                    OR COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy]) LIKE @SearchPattern ESCAPE N'\');

                ;WITH Filtered AS
                (
                    SELECT
                        auditLog.[Id], auditLog.[EntityName], auditLog.[Action], auditLog.[EntityKey],
                        auditLog.[ChangedColumns], auditLog.[OldValues], auditLog.[NewValues],
                        auditLog.[ChangedByUserId],
                        COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy], N'System') AS [ChangedByName],
                        auditLog.[TraceId], auditLog.[IpAddress], auditLog.[ChangedAtUtc]
                    FROM [dbo].[AuditLog] auditLog
                    LEFT JOIN [dbo].[Users] changedByUser ON changedByUser.[Id] = auditLog.[ChangedByUserId]
                    WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
                      AND (@Action IS NULL OR auditLog.[Action] = @Action)
                      AND (@FromUtc IS NULL OR auditLog.[ChangedAtUtc] >= @FromUtc)
                      AND (@ToUtc IS NULL OR auditLog.[ChangedAtUtc] <= @ToUtc)
                      AND (@ChangedByPattern IS NULL OR COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy], N'System') LIKE @ChangedByPattern ESCAPE N'\')
                      AND (@SearchPattern IS NULL
                        OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[EntityKey] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[OldValues] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[NewValues] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[TraceId] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[IpAddress] LIKE @SearchPattern ESCAPE N'\'
                        OR COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy]) LIKE @SearchPattern ESCAPE N'\')
                    ORDER BY
                        CASE WHEN @SortBy = N'changedatutc' AND @SortDirection = N'asc' THEN auditLog.[ChangedAtUtc] END ASC,
                        CASE WHEN @SortBy = N'changedatutc' AND @SortDirection = N'desc' THEN auditLog.[ChangedAtUtc] END DESC,
                        CASE WHEN @SortBy = N'entityname' AND @SortDirection = N'asc' THEN auditLog.[EntityName] END ASC,
                        CASE WHEN @SortBy = N'entityname' AND @SortDirection = N'desc' THEN auditLog.[EntityName] END DESC,
                        CASE WHEN @SortBy = N'action' AND @SortDirection = N'asc' THEN auditLog.[Action] END ASC,
                        CASE WHEN @SortBy = N'action' AND @SortDirection = N'desc' THEN auditLog.[Action] END DESC,
                        CASE WHEN @SortBy = N'changedbyname' AND @SortDirection = N'asc' THEN COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy], N'System') END ASC,
                        CASE WHEN @SortBy = N'changedbyname' AND @SortDirection = N'desc' THEN COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy], N'System') END DESC,
                        CASE WHEN @SortDirection = N'asc' THEN auditLog.[Id] END ASC,
                        auditLog.[Id] DESC
                    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY
                )
                SELECT
                    filtered.[Id], filtered.[EntityName],
                    COALESCE(
                        entityPermit.[PermitNumber], entityRisk.[PreRiskAssessmentNumber], entityWorkflow.[Name],
                        entityRole.[Name], entityListItem.[ItemName], entityDepartment.[Name], entityBranch.[Name],
                        entityModule.[Name], entityMenu.[Name], COALESCE(entityUser.[DisplayName], entityUser.[UserName]),
                        CONCAT(filtered.[EntityName], CASE WHEN entityKey.[EntityId] IS NULL THEN N'' ELSE CONCAT(N' #', entityKey.[EntityId]) END)
                    ) AS [EntityDisplayName],
                    filtered.[Action], filtered.[EntityKey], filtered.[ChangedColumns], filtered.[OldValues], filtered.[NewValues],
                    N'{}' AS [RelatedNames], filtered.[ChangedByUserId], filtered.[ChangedByName],
                    filtered.[TraceId], filtered.[IpAddress], filtered.[ChangedAtUtc]
                FROM Filtered filtered
                OUTER APPLY (SELECT TRY_CONVERT(bigint, JSON_VALUE(
                    CASE WHEN ISJSON(filtered.[EntityKey]) = 1 THEN filtered.[EntityKey] ELSE N'{}' END,
                    '$.Id')) AS [EntityId]) entityKey
                LEFT JOIN [dbo].[PermitApplication] entityPermit ON filtered.[EntityName] = N'PermitApplication' AND entityPermit.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[RiskAssessment] entityRisk ON filtered.[EntityName] = N'RiskAssessment' AND entityRisk.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[ApprovalWorkflow] entityWorkflow ON filtered.[EntityName] = N'ApprovalWorkflow' AND entityWorkflow.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[Role] entityRole ON filtered.[EntityName] = N'Role' AND entityRole.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[ListItem] entityListItem ON filtered.[EntityName] = N'ListItem' AND entityListItem.[ListItemId] = entityKey.[EntityId]
                LEFT JOIN [dbo].[Department] entityDepartment ON filtered.[EntityName] = N'Department' AND entityDepartment.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[OfficeBranch] entityBranch ON filtered.[EntityName] = N'OfficeBranch' AND entityBranch.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[ApplicationModule] entityModule ON filtered.[EntityName] = N'ApplicationModule' AND entityModule.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[ModuleMenu] entityMenu ON filtered.[EntityName] = N'ModuleMenu' AND entityMenu.[Id] = entityKey.[EntityId]
                LEFT JOIN [dbo].[Users] entityUser ON filtered.[EntityName] = N'Users' AND entityUser.[Id] = entityKey.[EntityId]
                ORDER BY
                    CASE WHEN @SortBy = N'changedatutc' AND @SortDirection = N'asc' THEN filtered.[ChangedAtUtc] END ASC,
                    CASE WHEN @SortBy = N'changedatutc' AND @SortDirection = N'desc' THEN filtered.[ChangedAtUtc] END DESC,
                    CASE WHEN @SortBy = N'entityname' AND @SortDirection = N'asc' THEN filtered.[EntityName] END ASC,
                    CASE WHEN @SortBy = N'entityname' AND @SortDirection = N'desc' THEN filtered.[EntityName] END DESC,
                    CASE WHEN @SortBy = N'action' AND @SortDirection = N'asc' THEN filtered.[Action] END ASC,
                    CASE WHEN @SortBy = N'action' AND @SortDirection = N'desc' THEN filtered.[Action] END DESC,
                    CASE WHEN @SortBy = N'changedbyname' AND @SortDirection = N'asc' THEN filtered.[ChangedByName] END ASC,
                    CASE WHEN @SortBy = N'changedbyname' AND @SortDirection = N'desc' THEN filtered.[ChangedByName] END DESC,
                    CASE WHEN @SortDirection = N'asc' THEN filtered.[Id] END ASC,
                    filtered.[Id] DESC;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpAuditLogsAdminGet];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpAuditLogFilterOptionsGet];");
    }
}
