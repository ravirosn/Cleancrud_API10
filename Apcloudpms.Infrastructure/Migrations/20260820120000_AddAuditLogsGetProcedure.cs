using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820120000_AddAuditLogsGetProcedure")]
public partial class AddAuditLogsGetProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE [dbo].[SpAuditLogsGet]
                @PageNumber int = 1,
                @PageSize int = 20,
                @SearchTerm nvarchar(200) = NULL,
                @EntityName nvarchar(128) = NULL,
                @Action nvarchar(20) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                IF @PageNumber < 1
                    THROW 50010, 'PageNumber must be greater than zero.', 1;

                IF @PageSize < 1 OR @PageSize > 100
                    THROW 50011, 'PageSize must be between 1 and 100.', 1;

                SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
                SET @EntityName = NULLIF(LTRIM(RTRIM(@EntityName)), N'');
                SET @Action = NULLIF(LTRIM(RTRIM(@Action)), N'');

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
                FROM [dbo].[AuditLog] AS auditLog
                LEFT JOIN [dbo].[Users] AS changedByUser
                    ON changedByUser.[Id] = auditLog.[ChangedByUserId]
                WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
                  AND (@Action IS NULL OR auditLog.[Action] = @Action)
                  AND (@SearchPattern IS NULL
                    OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[ChangedBy] LIKE @SearchPattern ESCAPE N'\'
                    OR changedByUser.[DisplayName] LIKE @SearchPattern ESCAPE N'\'
                    OR changedByUser.[UserName] LIKE @SearchPattern ESCAPE N'\'
                    OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\');

                ;WITH PagedAuditLogs AS
                (
                    SELECT
                        auditLog.[Id],
                        auditLog.[EntityName],
                        auditLog.[Action],
                        auditLog.[EntityKey],
                        auditLog.[ChangedColumns],
                        auditLog.[OldValues],
                        auditLog.[NewValues],
                        auditLog.[ChangedByUserId],
                        COALESCE(changedByUser.[DisplayName], changedByUser.[UserName], auditLog.[ChangedBy])
                            AS [ChangedByName],
                        auditLog.[TraceId],
                        auditLog.[IpAddress],
                        auditLog.[ChangedAtUtc]
                    FROM [dbo].[AuditLog] AS auditLog
                    LEFT JOIN [dbo].[Users] AS changedByUser
                        ON changedByUser.[Id] = auditLog.[ChangedByUserId]
                    WHERE (@EntityName IS NULL OR auditLog.[EntityName] = @EntityName)
                      AND (@Action IS NULL OR auditLog.[Action] = @Action)
                      AND (@SearchPattern IS NULL
                        OR auditLog.[EntityName] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[Action] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[ChangedBy] LIKE @SearchPattern ESCAPE N'\'
                        OR changedByUser.[DisplayName] LIKE @SearchPattern ESCAPE N'\'
                        OR changedByUser.[UserName] LIKE @SearchPattern ESCAPE N'\'
                        OR auditLog.[ChangedColumns] LIKE @SearchPattern ESCAPE N'\')
                    ORDER BY auditLog.[ChangedAtUtc] DESC, auditLog.[Id] DESC
                    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY
                )
                SELECT
                    paged.[Id],
                    paged.[EntityName],
                    COALESCE(
                        entityPermit.[PermitNumber],
                        entityRiskAssessment.[PreRiskAssessmentNumber],
                        entityWorkflow.[Name],
                        entityRole.[Name],
                        entityListItem.[ItemName],
                        entityDepartment.[Name],
                        entityBranch.[Name],
                        entityModule.[Name],
                        entityMenu.[Name],
                        COALESCE(entityUser.[DisplayName], entityUser.[UserName]),
                        relatedPermit.[PermitNumber],
                        relatedWorkflow.[Name],
                        COALESCE(relatedUser.[DisplayName], relatedUser.[UserName]),
                        CONCAT(paged.[EntityName],
                            CASE WHEN keys.[EntityId] IS NULL THEN N''
                                 ELSE CONCAT(N' #', keys.[EntityId]) END)) AS [EntityDisplayName],
                    paged.[Action],
                    paged.[EntityKey],
                    paged.[ChangedColumns],
                    paged.[OldValues],
                    paged.[NewValues],
                    JSON_QUERY((
                        SELECT
                            COALESCE(relatedUser.[DisplayName], relatedUser.[UserName]) AS [UserId],
                            COALESCE(createdBy.[DisplayName], createdBy.[UserName]) AS [CreatedByUserId],
                            COALESCE(updatedBy.[DisplayName], updatedBy.[UserName]) AS [UpdatedByUserId],
                            COALESCE(actionedBy.[DisplayName], actionedBy.[UserName]) AS [ActionedByUserId],
                            COALESCE(completionBy.[DisplayName], completionBy.[UserName]) AS [CompletionApprovedBy],
                            COALESCE(cancelledBy.[DisplayName], cancelledBy.[UserName]) AS [CancelledBy],
                            permitType.[ItemName] AS [PermitTypeListItemId],
                            permitStatus.[ItemName] AS [PermitStatusListItemId],
                            riskStatus.[ItemName] AS [RiskAssessmentStatusListItemId],
                            primaryRole.[Name] AS [PrimaryApproverRoleId],
                            alternateRole.[Name] AS [AlternateApproverRoleId],
                            relatedWorkflow.[Name] AS [ApprovalWorkflowId],
                            relatedPermit.[PermitNumber] AS [PermitApplicationId],
                            relatedRiskAssessment.[PreRiskAssessmentNumber] AS [RiskAssessmentId],
                            relatedDepartment.[Name] AS [DepartmentId],
                            relatedBranch.[Name] AS [OfficeBranchId],
                            relatedModule.[Name] AS [ApplicationModuleId],
                            relatedMenu.[Name] AS [ParentMenuId]
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                    )) AS [RelatedNames],
                    paged.[ChangedByUserId],
                    paged.[ChangedByName],
                    paged.[TraceId],
                    paged.[IpAddress],
                    paged.[ChangedAtUtc]
                FROM PagedAuditLogs AS paged
                OUTER APPLY
                (
                    SELECT
                        TRY_CONVERT(bigint, JSON_VALUE(paged.[EntityKey], '$.Id')) AS [EntityId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.UserId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.UserId'))) AS [UserId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CreatedByUserId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CreatedByUserId'))) AS [CreatedByUserId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.UpdatedByUserId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.UpdatedByUserId'))) AS [UpdatedByUserId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ActionedByUserId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ActionedByUserId'))) AS [ActionedByUserId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CompletionApprovedBy')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CompletionApprovedBy'))) AS [CompletionApprovedBy],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.CancelledBy')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.CancelledBy'))) AS [CancelledBy],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PermitTypeListItemId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PermitTypeListItemId'))) AS [PermitTypeListItemId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PermitStatusListItemId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PermitStatusListItemId'))) AS [PermitStatusListItemId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.RiskAssessmentStatusListItemId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.RiskAssessmentStatusListItemId'))) AS [RiskAssessmentStatusListItemId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.PrimaryApproverRoleId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.PrimaryApproverRoleId'))) AS [PrimaryApproverRoleId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.AlternateApproverRoleId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.AlternateApproverRoleId'))) AS [AlternateApproverRoleId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ApprovalWorkflowId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ApprovalWorkflowId'))) AS [ApprovalWorkflowId],
                        COALESCE(
                            TRY_CONVERT(bigint, JSON_VALUE(paged.[NewValues], '$.PermitApplicationId')),
                            TRY_CONVERT(bigint, JSON_VALUE(paged.[OldValues], '$.PermitApplicationId')),
                            TRY_CONVERT(bigint, JSON_VALUE(paged.[EntityKey], '$.PermitApplicationId'))) AS [PermitApplicationId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.RiskAssessmentId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.RiskAssessmentId'))) AS [RiskAssessmentId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.DepartmentId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.DepartmentId'))) AS [DepartmentId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.OfficeBranchId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.OfficeBranchId'))) AS [OfficeBranchId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ApplicationModuleId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ApplicationModuleId'))) AS [ApplicationModuleId],
                        COALESCE(
                            TRY_CONVERT(int, JSON_VALUE(paged.[NewValues], '$.ParentMenuId')),
                            TRY_CONVERT(int, JSON_VALUE(paged.[OldValues], '$.ParentMenuId'))) AS [ParentMenuId]
                ) AS keys
                LEFT JOIN [dbo].[PermitApplication] AS entityPermit
                    ON paged.[EntityName] = N'PermitApplication' AND entityPermit.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[RiskAssessment] AS entityRiskAssessment
                    ON paged.[EntityName] = N'RiskAssessment' AND entityRiskAssessment.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[ApprovalWorkflow] AS entityWorkflow
                    ON paged.[EntityName] = N'ApprovalWorkflow' AND entityWorkflow.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[Role] AS entityRole
                    ON paged.[EntityName] = N'Role' AND entityRole.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[ListItem] AS entityListItem
                    ON paged.[EntityName] = N'ListItem' AND entityListItem.[ListItemId] = keys.[EntityId]
                LEFT JOIN [dbo].[Department] AS entityDepartment
                    ON paged.[EntityName] = N'Department' AND entityDepartment.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[OfficeBranch] AS entityBranch
                    ON paged.[EntityName] = N'OfficeBranch' AND entityBranch.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[ApplicationModule] AS entityModule
                    ON paged.[EntityName] = N'ApplicationModule' AND entityModule.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[ModuleMenu] AS entityMenu
                    ON paged.[EntityName] = N'ModuleMenu' AND entityMenu.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[Users] AS entityUser
                    ON paged.[EntityName] = N'Users' AND entityUser.[Id] = keys.[EntityId]
                LEFT JOIN [dbo].[Users] AS relatedUser ON relatedUser.[Id] = keys.[UserId]
                LEFT JOIN [dbo].[Users] AS createdBy ON createdBy.[Id] = keys.[CreatedByUserId]
                LEFT JOIN [dbo].[Users] AS updatedBy ON updatedBy.[Id] = keys.[UpdatedByUserId]
                LEFT JOIN [dbo].[Users] AS actionedBy ON actionedBy.[Id] = keys.[ActionedByUserId]
                LEFT JOIN [dbo].[Users] AS completionBy ON completionBy.[Id] = keys.[CompletionApprovedBy]
                LEFT JOIN [dbo].[Users] AS cancelledBy ON cancelledBy.[Id] = keys.[CancelledBy]
                LEFT JOIN [dbo].[ListItem] AS permitType ON permitType.[ListItemId] = keys.[PermitTypeListItemId]
                LEFT JOIN [dbo].[ListItem] AS permitStatus ON permitStatus.[ListItemId] = keys.[PermitStatusListItemId]
                LEFT JOIN [dbo].[ListItem] AS riskStatus ON riskStatus.[ListItemId] = keys.[RiskAssessmentStatusListItemId]
                LEFT JOIN [dbo].[Role] AS primaryRole ON primaryRole.[Id] = keys.[PrimaryApproverRoleId]
                LEFT JOIN [dbo].[Role] AS alternateRole ON alternateRole.[Id] = keys.[AlternateApproverRoleId]
                LEFT JOIN [dbo].[ApprovalWorkflow] AS relatedWorkflow ON relatedWorkflow.[Id] = keys.[ApprovalWorkflowId]
                LEFT JOIN [dbo].[PermitApplication] AS relatedPermit ON relatedPermit.[Id] = keys.[PermitApplicationId]
                LEFT JOIN [dbo].[RiskAssessment] AS relatedRiskAssessment ON relatedRiskAssessment.[Id] = keys.[RiskAssessmentId]
                LEFT JOIN [dbo].[Department] AS relatedDepartment ON relatedDepartment.[Id] = keys.[DepartmentId]
                LEFT JOIN [dbo].[OfficeBranch] AS relatedBranch ON relatedBranch.[Id] = keys.[OfficeBranchId]
                LEFT JOIN [dbo].[ApplicationModule] AS relatedModule ON relatedModule.[Id] = keys.[ApplicationModuleId]
                LEFT JOIN [dbo].[ModuleMenu] AS relatedMenu ON relatedMenu.[Id] = keys.[ParentMenuId]
                ORDER BY paged.[ChangedAtUtc] DESC, paged.[Id] DESC;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpAuditLogsGet];");
    }
}
