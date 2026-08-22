using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260821100000_AddPermitApprovalSubmissionStatuses")]
public partial class AddPermitApprovalSubmissionStatuses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @PermitStatusCategoryId int;

            SELECT @PermitStatusCategoryId = [ListItemCategoryId]
            FROM [dbo].[ListItemCategory]
            WHERE [Code] = N'PERMIT_STATUS';

            IF @PermitStatusCategoryId IS NULL
                THROW 50010, 'The PERMIT_STATUS list item category is not configured.', 1;

            IF NOT EXISTS
            (
                SELECT 1
                FROM [dbo].[ListItem]
                WHERE [ListItemCategoryId] = @PermitStatusCategoryId
                  AND [SystemName] = N'FINALIZED_FOR_APPROVAL'
            )
            BEGIN
                INSERT INTO [dbo].[ListItem]
                (
                    [ListItemCategoryId], [SystemName], [ItemName], [Description],
                    [DisplayOrder], [IsVisible], [CreatedAtUtc]
                )
                VALUES
                (
                    @PermitStatusCategoryId, N'FINALIZED_FOR_APPROVAL',
                    N'Finalized For Approval',
                    N'The permit is complete and ready for its risk assessment to be submitted.',
                    20, 1, SYSUTCDATETIME()
                );
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM [dbo].[ListItem]
                WHERE [ListItemCategoryId] = @PermitStatusCategoryId
                  AND [SystemName] = N'PERMIT_SUBMITTED_FOR_APPROVAL'
            )
            BEGIN
                INSERT INTO [dbo].[ListItem]
                (
                    [ListItemCategoryId], [SystemName], [ItemName], [Description],
                    [DisplayOrder], [IsVisible], [CreatedAtUtc]
                )
                VALUES
                (
                    @PermitStatusCategoryId, N'PERMIT_SUBMITTED_FOR_APPROVAL',
                    N'Permit Submitted For Approval',
                    N'The permit approval workflow has started.',
                    30, 1, SYSUTCDATETIME()
                );
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Status rows may be referenced by permit applications after this migration runs.
        // Keep the reference data to avoid breaking those foreign keys during a rollback.
    }
}
