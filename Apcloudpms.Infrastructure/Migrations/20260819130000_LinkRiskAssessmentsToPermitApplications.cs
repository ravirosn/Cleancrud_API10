using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819130000_LinkRiskAssessmentsToPermitApplications")]
public partial class LinkRiskAssessmentsToPermitApplications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PermitApplication_RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication",
            column: "RiskAssessmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_PermitApplication_RiskAssessment_RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication",
            column: "RiskAssessmentId",
            principalSchema: "dbo",
            principalTable: "RiskAssessment",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(BuildInsertProcedureSql(createPermitApplications: true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(BuildInsertProcedureSql(createPermitApplications: false));

        migrationBuilder.DropForeignKey(
            name: "FK_PermitApplication_RiskAssessment_RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication");

        migrationBuilder.DropIndex(
            name: "IX_PermitApplication_RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication");

        migrationBuilder.DropColumn(
            name: "RiskAssessmentId",
            schema: "dbo",
            table: "PermitApplication");
    }

    private static string BuildInsertProcedureSql(bool createPermitApplications)
    {
        var permitStatusDeclaration = createPermitApplications
            ? "DECLARE @PermitDraftStatusId int;"
            : string.Empty;
        var permitStatusLookup = createPermitApplications
            ? """
              SELECT @PermitDraftStatusId = item.[ListItemId]
              FROM [dbo].[ListItem] AS item
              INNER JOIN [dbo].[ListItemCategory] AS category
                  ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
              WHERE category.[Code] = N'PERMIT_STATUS'
                AND item.[SystemName] = N'DRAFT';

              IF @PermitDraftStatusId IS NULL
                  THROW 50004, 'The Draft permit status is not configured.', 1;
              """
            : string.Empty;
        var permitInsert = createPermitApplications
            ? """
              INSERT INTO [dbo].[PermitApplication]
              (
                  [RiskAssessmentId], [PermitNumber], [IssueDate],
                  [PermitIssuerName], [PermitIssuerContactNumber],
                  [PermitReceiverName], [PermitReceiverContactNumber],
                  [PreRiskAssessmentNumber], [WorkLocation], [WorkDescription],
                  [SpecialInstructions], [WorkHeightBelowSurface],
                  [PermitTypeListItemId], [PermitStatusListItemId],
                  [SubmittedAtUtc], [CreatedByUserId], [UpdatedByUserId],
                  [CreatedAtUtc], [UpdatedAtUtc]
              )
              SELECT
                  @RiskAssessmentId,
                  CONCAT(N'RA-', @RiskAssessmentId, N'-', specialPermit.[ListItemId]),
                  @IssueDate,
                  @PermitIssuerName,
                  NULL,
                  @PermitReceiverName,
                  NULL,
                  @PreRiskAssessmentNumber,
                  @LocationOfWork,
                  COALESCE(@DescriptionOfWork, N''),
                  @SpecialInstructions,
                  NULL,
                  specialPermit.[ListItemId],
                  @PermitDraftStatusId,
                  NULL,
                  @CreatedBy,
                  NULL,
                  @Now,
                  NULL
              FROM @SpecialPermits AS specialPermit
              WHERE specialPermit.[IsSelected] = 1;
              """
            : string.Empty;

        return $$"""
            CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
                @PreRiskAssessmentNumber nvarchar(50),
                @IssueDate date,
                @PermitIssuerName nvarchar(100),
                @PermitReceiverName nvarchar(100),
                @AreaResponsibleName nvarchar(100),
                @LocationOfWork nvarchar(255),
                @DescriptionOfWork nvarchar(max) = NULL,
                @SpecialInstructions nvarchar(max) = NULL,
                @OtherEquipmentsPPE nvarchar(500) = NULL,
                @OtherProtectionMeasures nvarchar(500) = NULL,
                @PlannedStartDateTime datetime2(0) = NULL,
                @PlannedEndDateTime datetime2(0) = NULL,
                @CreatedBy int,
                @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
                @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
                @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
                @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                DECLARE @DraftStatusId int;
                {{permitStatusDeclaration}}
                DECLARE @RiskAssessmentId int;
                DECLARE @Now datetime2(0) = SYSUTCDATETIME();

                SELECT @DraftStatusId = item.[ListItemId]
                FROM [dbo].[ListItem] AS item
                INNER JOIN [dbo].[ListItemCategory] AS category
                    ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
                WHERE category.[Code] = N'RISK_ASSESSMENT_STATUS'
                  AND item.[SystemName] = N'DRAFT';

                IF @DraftStatusId IS NULL
                    THROW 50003, 'The Draft risk assessment status is not configured.', 1;

                {{permitStatusLookup}}

                BEGIN TRY
                    BEGIN TRANSACTION;

                    INSERT INTO [dbo].[RiskAssessment]
                    (
                        [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName],
                        [PermitReceiverName], [AreaResponsibleName], [LocationOfWork],
                        [DescriptionOfWork], [SpecialInstructions], [OtherEquipmentsPPE],
                        [OtherProtectionMeasures], [PlannedStartDateTime], [PlannedEndDateTime],
                        [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
                        [CreatedAtUtc], [UpdatedAtUtc]
                    )
                    VALUES
                    (
                        @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName,
                        @PermitReceiverName, @AreaResponsibleName, @LocationOfWork,
                        @DescriptionOfWork, @SpecialInstructions, @OtherEquipmentsPPE,
                        @OtherProtectionMeasures, @PlannedStartDateTime, @PlannedEndDateTime,
                        @DraftStatusId, @CreatedBy, NULL, @Now, @Now
                    );

                    SET @RiskAssessmentId = CONVERT(int, SCOPE_IDENTITY());

                    INSERT INTO [dbo].[RiskAssessmentAdditionalPPE]
                        ([RiskAssessmentId], [AdditionalProtectiveMeasuresListItemId], [IsSelected])
                    SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @AdditionalPpe;
                    INSERT INTO [dbo].[RiskAssessmentHazardCategories]
                        ([RiskAssessmentId], [HazardCategoriesListItemId], [IsSelected])
                    SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @HazardCategories;
                    INSERT INTO [dbo].[RiskAssessmentPPE]
                        ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
                    SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @PersonalProtectiveEquipment;
                    INSERT INTO [dbo].[RiskAssessmentSpecialPermit]
                        ([RiskAssessmentId], [SpecialPermitListItemId], [IsSelected])
                    SELECT @RiskAssessmentId, [ListItemId], [IsSelected] FROM @SpecialPermits;

                    {{permitInsert}}

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;

                SELECT @RiskAssessmentId AS [RiskAssessmentId],
                       @DraftStatusId AS [RiskAssessmentStatusListItemId],
                       N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
            END;
            """;
    }
}
