using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRiskAssessmentContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BuildInsertProcedureSql(includeContacts: false));
            migrationBuilder.Sql(BuildUpdateProcedureSql(includeContacts: false));

            migrationBuilder.DropColumn(
                name: "AreaResponsibleContact",
                schema: "dbo",
                table: "RiskAssessment");

            migrationBuilder.DropColumn(
                name: "PermitIssuerContact",
                schema: "dbo",
                table: "RiskAssessment");

            migrationBuilder.DropColumn(
                name: "PermitReceiverContact",
                schema: "dbo",
                table: "RiskAssessment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaResponsibleContact",
                schema: "dbo",
                table: "RiskAssessment",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitIssuerContact",
                schema: "dbo",
                table: "RiskAssessment",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitReceiverContact",
                schema: "dbo",
                table: "RiskAssessment",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql(BuildInsertProcedureSql(includeContacts: true));
            migrationBuilder.Sql(BuildUpdateProcedureSql(includeContacts: true));
        }

        private static string BuildInsertProcedureSql(bool includeContacts)
        {
            var contactParameters = includeContacts
                ? """
                  @PermitIssuerContact nvarchar(30) = NULL,
                  @PermitReceiverContact nvarchar(30) = NULL,
                  @AreaResponsibleContact nvarchar(30) = NULL,
                  """
                : string.Empty;
            var contactColumns = includeContacts
                ? ", [PermitIssuerContact], [PermitReceiverContact], [AreaResponsibleContact]"
                : string.Empty;
            var contactValues = includeContacts
                ? ", @PermitIssuerContact, @PermitReceiverContact, @AreaResponsibleContact"
                : string.Empty;

            return $$"""
                CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
                    @PreRiskAssessmentNumber nvarchar(50),
                    @IssueDate date,
                    @PermitIssuerName nvarchar(100),
                    @PermitReceiverName nvarchar(100),
                    @AreaResponsibleName nvarchar(100),
                    {{contactParameters}}@LocationOfWork nvarchar(255),
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
                            [PreRiskAssessmentNumber], [IssueDate], [PermitIssuerName],
                            [PermitReceiverName], [AreaResponsibleName]{{contactColumns}},
                            [LocationOfWork], [DescriptionOfWork], [SpecialInstructions],
                            [OtherEquipmentsPPE], [OtherProtectionMeasures],
                            [PlannedStartDateTime], [PlannedEndDateTime],
                            [RiskAssessmentStatusListItemId], [CreatedBy], [ModifiedBy],
                            [CreatedAtUtc], [UpdatedAtUtc]
                        )
                        VALUES
                        (
                            @PreRiskAssessmentNumber, @IssueDate, @PermitIssuerName,
                            @PermitReceiverName, @AreaResponsibleName{{contactValues}},
                            @LocationOfWork, @DescriptionOfWork, @SpecialInstructions,
                            @OtherEquipmentsPPE, @OtherProtectionMeasures,
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
        }

        private static string BuildUpdateProcedureSql(bool includeContacts)
        {
            var contactParameters = includeContacts
                ? """
                  @PermitIssuerContact nvarchar(30) = NULL,
                  @PermitReceiverContact nvarchar(30) = NULL,
                  @AreaResponsibleContact nvarchar(30) = NULL,
                  """
                : string.Empty;
            var contactAssignments = includeContacts
                ? """
                      [PermitIssuerContact] = @PermitIssuerContact,
                      [PermitReceiverContact] = @PermitReceiverContact,
                      [AreaResponsibleContact] = @AreaResponsibleContact,
                  """
                : string.Empty;

            return $$"""
                CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentUpd]
                    @RiskAssessmentId int,
                    @PreRiskAssessmentNumber nvarchar(50),
                    @IssueDate date,
                    @PermitIssuerName nvarchar(100),
                    @PermitReceiverName nvarchar(100),
                    @AreaResponsibleName nvarchar(100),
                    {{contactParameters}}@LocationOfWork nvarchar(255),
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
                            [PermitReceiverName] = @PermitReceiverName,
                            [AreaResponsibleName] = @AreaResponsibleName,
                            {{contactAssignments}}[LocationOfWork] = @LocationOfWork,
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
    }
}
