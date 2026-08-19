using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CleanCrud.Infrastructure.Data;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819010000_AddRiskAssessmentWorkflowAndProcedures")]
public partial class AddRiskAssessmentWorkflowAndProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CreatedBy", schema: "dbo", table: "RiskAssessment", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "ModifiedBy", schema: "dbo", table: "RiskAssessment", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "OtherEquipmentsPPE", schema: "dbo", table: "RiskAssessment",
            type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "OtherProtectionMeasures", schema: "dbo", table: "RiskAssessment",
            type: "nvarchar(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "RiskAssessmentStatusListItemId", schema: "dbo", table: "RiskAssessment",
            type: "int", nullable: true);

        migrationBuilder.Sql("""
            DECLARE @CategoryId int;
            SELECT @CategoryId = [ListItemCategoryId]
            FROM [dbo].[ListItemCategory]
            WHERE [Code] = N'RISK_ASSESSMENT_STATUS';

            IF @CategoryId IS NULL
            BEGIN
                INSERT INTO [dbo].[ListItemCategory]
                    ([Code], [CategoryName], [Description], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
                VALUES
                    (N'RISK_ASSESSMENT_STATUS', N'RiskAssessmentStatus',
                     N'Workflow statuses for risk assessments.', 1, SYSUTCDATETIME(), NULL);
                SET @CategoryId = CONVERT(int, SCOPE_IDENTITY());
            END;

            DECLARE @Statuses TABLE
            (
                [SystemName] nvarchar(50) NOT NULL,
                [ItemName] nvarchar(100) NOT NULL,
                [DisplayOrder] int NOT NULL
            );
            INSERT INTO @Statuses ([SystemName], [ItemName], [DisplayOrder])
            VALUES
                (N'DRAFT', N'Draft', 1),
                (N'SUBMITTED_FOR_APPROVAL', N'Submitted For Approval', 2),
                (N'APPROVED', N'Approved', 3),
                (N'REJECTED', N'Rejected', 4),
                (N'DELETED', N'Deleted', 5);

            INSERT INTO [dbo].[ListItem]
                ([ListItemCategoryId], [SystemName], [ItemName], [Description],
                 [DisplayOrder], [IsVisible], [CreatedAtUtc], [UpdatedAtUtc])
            SELECT @CategoryId, source.[SystemName], source.[ItemName], NULL,
                   source.[DisplayOrder], 1, SYSUTCDATETIME(), NULL
            FROM @Statuses AS source
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [dbo].[ListItem] AS target
                WHERE target.[ListItemCategoryId] = @CategoryId
                  AND target.[SystemName] = source.[SystemName]
            );

            DECLARE @DraftStatusId int;
            SELECT @DraftStatusId = [ListItemId]
            FROM [dbo].[ListItem]
            WHERE [ListItemCategoryId] = @CategoryId AND [SystemName] = N'DRAFT';

            UPDATE [dbo].[RiskAssessment]
            SET [RiskAssessmentStatusListItemId] = @DraftStatusId
            WHERE [RiskAssessmentStatusListItemId] IS NULL;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "RiskAssessmentStatusListItemId", schema: "dbo", table: "RiskAssessment",
            type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_CreatedBy", schema: "dbo", table: "RiskAssessment", column: "CreatedBy");
        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_ModifiedBy", schema: "dbo", table: "RiskAssessment", column: "ModifiedBy");
        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_RiskAssessmentStatusListItemId_CreatedAtUtc",
            schema: "dbo", table: "RiskAssessment",
            columns: new[] { "RiskAssessmentStatusListItemId", "CreatedAtUtc" });

        migrationBuilder.AddForeignKey(
            name: "FK_RiskAssessment_ListItem_RiskAssessmentStatusListItemId",
            schema: "dbo", table: "RiskAssessment", column: "RiskAssessmentStatusListItemId",
            principalSchema: "dbo", principalTable: "ListItem", principalColumn: "ListItemId",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_RiskAssessment_Users_CreatedBy",
            schema: "dbo", table: "RiskAssessment", column: "CreatedBy",
            principalTable: "Users", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_RiskAssessment_Users_ModifiedBy",
            schema: "dbo", table: "RiskAssessment", column: "ModifiedBy",
            principalTable: "Users", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("""
            CREATE TYPE [dbo].[RiskAssessmentSelectionTableType] AS TABLE
            (
                [ListItemId] int NOT NULL,
                [IsSelected] bit NOT NULL
            );
            """);

        migrationBuilder.Sql(InsertProcedureSql);
        migrationBuilder.Sql(UpdateProcedureSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentUpd];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentIns];");
        migrationBuilder.Sql("DROP TYPE IF EXISTS [dbo].[RiskAssessmentSelectionTableType];");

        migrationBuilder.DropForeignKey(
            name: "FK_RiskAssessment_ListItem_RiskAssessmentStatusListItemId",
            schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropForeignKey(
            name: "FK_RiskAssessment_Users_CreatedBy",
            schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropForeignKey(
            name: "FK_RiskAssessment_Users_ModifiedBy",
            schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(
            name: "IX_RiskAssessment_CreatedBy", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(
            name: "IX_RiskAssessment_ModifiedBy", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(
            name: "IX_RiskAssessment_RiskAssessmentStatusListItemId_CreatedAtUtc",
            schema: "dbo", table: "RiskAssessment");

        migrationBuilder.DropColumn(name: "CreatedBy", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(name: "ModifiedBy", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(name: "OtherEquipmentsPPE", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(name: "OtherProtectionMeasures", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(
            name: "RiskAssessmentStatusListItemId", schema: "dbo", table: "RiskAssessment");

        migrationBuilder.Sql("""
            DECLARE @CategoryId int;
            SELECT @CategoryId = [ListItemCategoryId]
            FROM [dbo].[ListItemCategory]
            WHERE [Code] = N'RISK_ASSESSMENT_STATUS';
            DELETE FROM [dbo].[ListItem] WHERE [ListItemCategoryId] = @CategoryId;
            DELETE FROM [dbo].[ListItemCategory] WHERE [ListItemCategoryId] = @CategoryId;
            """);
    }

    private const string InsertProcedureSql = """
        CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
            @PreRiskAssessmentNumber nvarchar(50),
            @IssueDate date,
            @PermitIssuerName nvarchar(100),
            @PermitIssuerContact nvarchar(30) = NULL,
            @PermitReceiverName nvarchar(100),
            @PermitReceiverContact nvarchar(30) = NULL,
            @AreaResponsibleName nvarchar(100),
            @AreaResponsibleContact nvarchar(30) = NULL,
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

            BEGIN TRY
                BEGIN TRANSACTION;

                INSERT INTO [dbo].[RiskAssessment]
                (
                    [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName], [PermitIssuerContact],
                    [PermitReceiverName], [PermitReceiverContact], [AreaResponsibleName],
                    [AreaResponsibleContact], [LocationOfWork], [DescriptionOfWork],
                    [SpecialInstructions], [OtherEquipmentsPPE], [OtherProtectionMeasures],
                    [PlannedStartDateTime], [PlannedEndDateTime],
                    [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
                    [CreatedAtUtc], [UpdatedAtUtc]
                )
                VALUES
                (
                    @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName, @PermitIssuerContact,
                    @PermitReceiverName, @PermitReceiverContact, @AreaResponsibleName,
                    @AreaResponsibleContact, @LocationOfWork, @DescriptionOfWork,
                    @SpecialInstructions, @OtherEquipmentsPPE, @OtherProtectionMeasures,
                    @PlannedStartDateTime, @PlannedEndDateTime,
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

    private const string UpdateProcedureSql = """
        CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentUpd]
            @RiskAssessmentId int,
            @PreRiskAssessmentNumber nvarchar(50),
            @IssueDate date,
            @PermitIssuerName nvarchar(100),
            @PermitIssuerContact nvarchar(30) = NULL,
            @PermitReceiverName nvarchar(100),
            @PermitReceiverContact nvarchar(30) = NULL,
            @AreaResponsibleName nvarchar(100),
            @AreaResponsibleContact nvarchar(30) = NULL,
            @LocationOfWork nvarchar(255),
            @DescriptionOfWork nvarchar(max) = NULL,
            @SpecialInstructions nvarchar(max) = NULL,
            @OtherEquipmentsPPE nvarchar(500) = NULL,
            @OtherProtectionMeasures nvarchar(500) = NULL,
            @PlannedStartDateTime datetime2(0) = NULL,
            @PlannedEndDateTime datetime2(0) = NULL,
            @ModifiedBy int,
            @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            DECLARE @CurrentStatusId int;
            DECLARE @Now datetime2(0) = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                SELECT @CurrentStatusId = [RiskAssessmentStatusListItemId]
                FROM [dbo].[RiskAssessment] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = @RiskAssessmentId;

                IF @CurrentStatusId IS NULL
                    THROW 50001, 'Risk assessment was not found.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM [dbo].[ListItem] AS item
                    INNER JOIN [dbo].[ListItemCategory] AS category
                        ON category.[ListItemCategoryId] = item.[ListItemCategoryId]
                    WHERE item.[ListItemId] = @CurrentStatusId
                      AND category.[Code] = N'RISK_ASSESSMENT_STATUS'
                      AND item.[SystemName] = N'DRAFT'
                )
                    THROW 50002, 'Only Draft risk assessments can be updated.', 1;

                UPDATE [dbo].[RiskAssessment]
                SET [PreRiskAssessmentNumber] = @PreRiskAssessmentNumber,
                    [IssueDate] = @IssueDate,
                    [PermitIssuerName] = @PermitIssuerName,
                    [PermitIssuerContact] = @PermitIssuerContact,
                    [PermitReceiverName] = @PermitReceiverName,
                    [PermitReceiverContact] = @PermitReceiverContact,
                    [AreaResponsibleName] = @AreaResponsibleName,
                    [AreaResponsibleContact] = @AreaResponsibleContact,
                    [LocationOfWork] = @LocationOfWork,
                    [DescriptionOfWork] = @DescriptionOfWork,
                    [SpecialInstructions] = @SpecialInstructions,
                    [OtherEquipmentsPPE] = @OtherEquipmentsPPE,
                    [OtherProtectionMeasures] = @OtherProtectionMeasures,
                    [PlannedStartDateTime] = @PlannedStartDateTime,
                    [PlannedEndDateTime] = @PlannedEndDateTime,
                    [ModifiedBy] = @ModifiedBy,
                    [UpdatedAtUtc] = @Now
                WHERE [Id] = @RiskAssessmentId;

                DELETE FROM [dbo].[RiskAssessmentAdditionalPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentHazardCategories] WHERE [RiskAssessmentId] = @RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentPPE] WHERE [RiskAssessmentId] = @RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentSpecialPermit] WHERE [RiskAssessmentId] = @RiskAssessmentId;

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

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;

            SELECT @RiskAssessmentId AS [RiskAssessmentId],
                   @CurrentStatusId AS [RiskAssessmentStatusListItemId],
                   N'Draft' AS [Status], @Now AS [UpdatedAtUtc];
        END;
        """;
}
