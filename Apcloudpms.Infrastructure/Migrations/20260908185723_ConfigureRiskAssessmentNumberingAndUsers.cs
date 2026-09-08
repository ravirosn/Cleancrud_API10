using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

public partial class ConfigureRiskAssessmentNumberingAndUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PreRiskAssessmentNumber",
            schema: "dbo",
            table: "RiskAssessment",
            newName: "RiskAssessmentNumber");

        migrationBuilder.RenameColumn(
            name: "PreRiskAssessmentNumber",
            schema: "dbo",
            table: "PermitApplication",
            newName: "RiskAssessmentNumber");

        migrationBuilder.AddColumn<int>(
            name: "PermitIssuerUserId",
            schema: "dbo",
            table: "RiskAssessment",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PermitReceiverUserId",
            schema: "dbo",
            table: "RiskAssessment",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE riskAssessment
            SET [PermitIssuerUserId] = issuer.[Id],
                [PermitReceiverUserId] = receiver.[Id]
            FROM [dbo].[RiskAssessment] riskAssessment
            OUTER APPLY
            (
                SELECT TOP (1) users.[Id]
                FROM [dbo].[Users] users
                WHERE LTRIM(RTRIM(COALESCE(users.[DisplayName], users.[UserName]))) =
                      LTRIM(RTRIM(riskAssessment.[PermitIssuerName]))
                   OR LTRIM(RTRIM(users.[UserName])) = LTRIM(RTRIM(riskAssessment.[PermitIssuerName]))
                ORDER BY CASE WHEN users.[IsActive] = 1 THEN 0 ELSE 1 END, users.[Id]
            ) issuer
            OUTER APPLY
            (
                SELECT TOP (1) users.[Id]
                FROM [dbo].[Users] users
                WHERE LTRIM(RTRIM(COALESCE(users.[DisplayName], users.[UserName]))) =
                      LTRIM(RTRIM(riskAssessment.[PermitReceiverName]))
                   OR LTRIM(RTRIM(users.[UserName])) = LTRIM(RTRIM(riskAssessment.[PermitReceiverName]))
                ORDER BY CASE WHEN users.[IsActive] = 1 THEN 0 ELSE 1 END, users.[Id]
            ) receiver;

            IF EXISTS
            (
                SELECT 1 FROM [dbo].[RiskAssessment]
                WHERE [PermitIssuerUserId] IS NULL OR [PermitReceiverUserId] IS NULL
            )
                THROW 50005, 'Existing risk-assessment issuer or receiver names could not be matched to users. Correct those names before applying this migration.', 1;

            IF EXISTS
            (
                SELECT [RiskAssessmentNumber]
                FROM [dbo].[RiskAssessment]
                GROUP BY [RiskAssessmentNumber]
                HAVING COUNT_BIG(1) > 1
            )
                THROW 50006, 'Existing risk-assessment numbers contain duplicates. Correct them before applying this migration.', 1;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "PermitIssuerUserId",
            schema: "dbo",
            table: "RiskAssessment",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "PermitReceiverUserId",
            schema: "dbo",
            table: "RiskAssessment",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.DropColumn(name: "PermitIssuerName", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(name: "PermitReceiverName", schema: "dbo", table: "RiskAssessment");

        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_RiskAssessmentNumber",
            schema: "dbo",
            table: "RiskAssessment",
            column: "RiskAssessmentNumber",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_PermitIssuerUserId",
            schema: "dbo",
            table: "RiskAssessment",
            column: "PermitIssuerUserId");
        migrationBuilder.CreateIndex(
            name: "IX_RiskAssessment_PermitReceiverUserId",
            schema: "dbo",
            table: "RiskAssessment",
            column: "PermitReceiverUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_RiskAssessment_Users_PermitIssuerUserId",
            schema: "dbo",
            table: "RiskAssessment",
            column: "PermitIssuerUserId",
            principalSchema: "dbo",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_RiskAssessment_Users_PermitReceiverUserId",
            schema: "dbo",
            table: "RiskAssessment",
            column: "PermitReceiverUserId",
            principalSchema: "dbo",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddCheckConstraint(
            name: "CK_FiscalYearSetting_NextNumbersNumeric",
            schema: "dbo",
            table: "FiscalYearSetting",
            sql: "LEN([NextRaNumber]) > 0 AND [NextRaNumber] NOT LIKE '%[^0-9]%' AND LEN([NextPaNumber]) > 0 AND [NextPaNumber] NOT LIKE '%[^0-9]%'");

        migrationBuilder.Sql(CreateGetProcedure());
        migrationBuilder.Sql(CreateInsertProcedure());
        migrationBuilder.Sql(CreateUpdateProcedure());
        migrationBuilder.Sql(ReplaceProcedureReferences("PreRiskAssessmentNumber", "RiskAssessmentNumber"));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentUpd];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentIns];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpRiskAssessmentGet];");

        migrationBuilder.DropCheckConstraint(
            name: "CK_FiscalYearSetting_NextNumbersNumeric",
            schema: "dbo",
            table: "FiscalYearSetting");
        migrationBuilder.DropForeignKey(name: "FK_RiskAssessment_Users_PermitIssuerUserId", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropForeignKey(name: "FK_RiskAssessment_Users_PermitReceiverUserId", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(name: "IX_RiskAssessment_RiskAssessmentNumber", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(name: "IX_RiskAssessment_PermitIssuerUserId", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropIndex(name: "IX_RiskAssessment_PermitReceiverUserId", schema: "dbo", table: "RiskAssessment");

        migrationBuilder.AddColumn<string>(name: "PermitIssuerName", schema: "dbo", table: "RiskAssessment", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PermitReceiverName", schema: "dbo", table: "RiskAssessment", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.Sql("""
            UPDATE riskAssessment
            SET [PermitIssuerName] = COALESCE(issuer.[DisplayName], issuer.[UserName]),
                [PermitReceiverName] = COALESCE(receiver.[DisplayName], receiver.[UserName])
            FROM [dbo].[RiskAssessment] riskAssessment
            INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = riskAssessment.[PermitIssuerUserId]
            INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = riskAssessment.[PermitReceiverUserId];
            """);
        migrationBuilder.AlterColumn<string>(name: "PermitIssuerName", schema: "dbo", table: "RiskAssessment", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "PermitReceiverName", schema: "dbo", table: "RiskAssessment", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
        migrationBuilder.DropColumn(name: "PermitIssuerUserId", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.DropColumn(name: "PermitReceiverUserId", schema: "dbo", table: "RiskAssessment");
        migrationBuilder.RenameColumn(name: "RiskAssessmentNumber", schema: "dbo", table: "RiskAssessment", newName: "PreRiskAssessmentNumber");
        migrationBuilder.RenameColumn(name: "RiskAssessmentNumber", schema: "dbo", table: "PermitApplication", newName: "PreRiskAssessmentNumber");
        migrationBuilder.Sql(ReplaceProcedureReferences("RiskAssessmentNumber", "PreRiskAssessmentNumber"));
    }

    private static string CreateGetProcedure() => """
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
            IF @SortBy NOT IN (N'riskAssessmentNumber', N'issueDate', N'permitIssuerName', N'permitReceiverName', N'areaResponsibleName', N'plannedStartDateTime', N'plannedEndDateTime', N'riskAssessmentStatus')
                THROW 50013, 'SortBy is not supported.', 1;
            IF @SortDirection NOT IN ('asc', 'desc') THROW 50014, 'SortDirection must be asc or desc.', 1;

            DECLARE @SearchPattern nvarchar(402) = NULL;
            IF @SearchTerm IS NOT NULL
                SET @SearchPattern = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(@SearchTerm, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

            SELECT COUNT_BIG(1) AS [TotalRecords]
            FROM [dbo].[RiskAssessment] riskAssessment
            INNER JOIN [dbo].[ListItem] statusItem ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
            INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = riskAssessment.[PermitIssuerUserId]
            INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = riskAssessment.[PermitReceiverUserId]
            WHERE @SearchPattern IS NULL
               OR riskAssessment.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
               OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N'\'
               OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N'\'
               OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
               OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\';

            SELECT riskAssessment.[Id], riskAssessment.[RiskAssessmentNumber], riskAssessment.[IssueDate],
                COALESCE(issuer.[DisplayName], issuer.[UserName]) AS [PermitIssuerName],
                COALESCE(receiver.[DisplayName], receiver.[UserName]) AS [PermitReceiverName],
                riskAssessment.[AreaResponsibleName], riskAssessment.[PlannedStartDateTime], riskAssessment.[PlannedEndDateTime],
                riskAssessment.[RiskAssessmentStatusListItemId], statusItem.[ItemName] AS [RiskAssessmentStatus]
            FROM [dbo].[RiskAssessment] riskAssessment
            INNER JOIN [dbo].[ListItem] statusItem ON statusItem.[ListItemId] = riskAssessment.[RiskAssessmentStatusListItemId]
            INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = riskAssessment.[PermitIssuerUserId]
            INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = riskAssessment.[PermitReceiverUserId]
            WHERE @SearchPattern IS NULL
               OR riskAssessment.[RiskAssessmentNumber] LIKE @SearchPattern ESCAPE N'\'
               OR COALESCE(issuer.[DisplayName], issuer.[UserName]) LIKE @SearchPattern ESCAPE N'\'
               OR COALESCE(receiver.[DisplayName], receiver.[UserName]) LIKE @SearchPattern ESCAPE N'\'
               OR riskAssessment.[AreaResponsibleName] LIKE @SearchPattern ESCAPE N'\'
               OR statusItem.[ItemName] LIKE @SearchPattern ESCAPE N'\'
            ORDER BY
                CASE WHEN @SortBy=N'riskAssessmentNumber' AND @SortDirection='asc' THEN riskAssessment.[RiskAssessmentNumber] END ASC,
                CASE WHEN @SortBy=N'riskAssessmentNumber' AND @SortDirection='desc' THEN riskAssessment.[RiskAssessmentNumber] END DESC,
                CASE WHEN @SortBy=N'issueDate' AND @SortDirection='asc' THEN riskAssessment.[IssueDate] END ASC,
                CASE WHEN @SortBy=N'issueDate' AND @SortDirection='desc' THEN riskAssessment.[IssueDate] END DESC,
                CASE WHEN @SortBy=N'permitIssuerName' AND @SortDirection='asc' THEN COALESCE(issuer.[DisplayName], issuer.[UserName]) END ASC,
                CASE WHEN @SortBy=N'permitIssuerName' AND @SortDirection='desc' THEN COALESCE(issuer.[DisplayName], issuer.[UserName]) END DESC,
                CASE WHEN @SortBy=N'permitReceiverName' AND @SortDirection='asc' THEN COALESCE(receiver.[DisplayName], receiver.[UserName]) END ASC,
                CASE WHEN @SortBy=N'permitReceiverName' AND @SortDirection='desc' THEN COALESCE(receiver.[DisplayName], receiver.[UserName]) END DESC,
                CASE WHEN @SortBy=N'areaResponsibleName' AND @SortDirection='asc' THEN riskAssessment.[AreaResponsibleName] END ASC,
                CASE WHEN @SortBy=N'areaResponsibleName' AND @SortDirection='desc' THEN riskAssessment.[AreaResponsibleName] END DESC,
                CASE WHEN @SortBy=N'plannedStartDateTime' AND @SortDirection='asc' THEN riskAssessment.[PlannedStartDateTime] END ASC,
                CASE WHEN @SortBy=N'plannedStartDateTime' AND @SortDirection='desc' THEN riskAssessment.[PlannedStartDateTime] END DESC,
                CASE WHEN @SortBy=N'plannedEndDateTime' AND @SortDirection='asc' THEN riskAssessment.[PlannedEndDateTime] END ASC,
                CASE WHEN @SortBy=N'plannedEndDateTime' AND @SortDirection='desc' THEN riskAssessment.[PlannedEndDateTime] END DESC,
                CASE WHEN @SortBy=N'riskAssessmentStatus' AND @SortDirection='asc' THEN statusItem.[ItemName] END ASC,
                CASE WHEN @SortBy=N'riskAssessmentStatus' AND @SortDirection='desc' THEN statusItem.[ItemName] END DESC,
                riskAssessment.[Id] DESC
            OFFSET (CONVERT(bigint,@PageNumber)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
        END;
        """;

    internal static string CreateInsertProcedure() => """
        CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentIns]
            @IssueDate date, @PermitIssuerUserId int, @PermitReceiverUserId int,
            @AreaResponsibleName nvarchar(100), @LocationOfWork nvarchar(255),
            @DescriptionOfWork nvarchar(max)=NULL, @SpecialInstructions nvarchar(max)=NULL,
            @OtherEquipmentsPPE nvarchar(500)=NULL, @OtherProtectionMeasures nvarchar(500)=NULL,
            @PlannedStartDateTime datetime2(0)=NULL, @PlannedEndDateTime datetime2(0)=NULL,
            @CreatedBy int,
            @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            DECLARE @DraftStatusId int, @PermitDraftStatusId int, @RiskAssessmentId int,
                    @Now datetime2(0)=SYSUTCDATETIME(), @RiskAssessmentNumber nvarchar(50),
                    @IssuerName nvarchar(200), @ReceiverName nvarchar(200),
                    @FiscalYearId int, @RaPrefix nvarchar(50), @PaPrefix nvarchar(50),
                    @NextRaNumber nvarchar(50), @NextPaNumber nvarchar(50),
                    @RaValue bigint, @PaValue bigint, @PermitCount int;

            SELECT @DraftStatusId=item.[ListItemId] FROM [dbo].[ListItem] item
            JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
            WHERE category.[Code]=N'RISK_ASSESSMENT_STATUS' AND item.[SystemName]=N'DRAFT';
            SELECT @PermitDraftStatusId=item.[ListItemId] FROM [dbo].[ListItem] item
            JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
            WHERE category.[Code]=N'PERMIT_STATUS' AND item.[SystemName]=N'PERMIT_DRAFT';
            IF @DraftStatusId IS NULL THROW 50003, 'The Draft risk assessment status is not configured.', 1;
            SELECT @PermitCount=COUNT(*) FROM @SpecialPermits WHERE [IsSelected]=1;
            IF @PermitCount>0 AND @PermitDraftStatusId IS NULL THROW 50004, 'The Draft permit status is not configured.', 1;

            BEGIN TRY
                BEGIN TRANSACTION;
                SELECT @IssuerName=COALESCE([DisplayName],[UserName]) FROM [dbo].[Users] WHERE [Id]=@PermitIssuerUserId AND [IsActive]=1;
                SELECT @ReceiverName=COALESCE([DisplayName],[UserName]) FROM [dbo].[Users] WHERE [Id]=@PermitReceiverUserId AND [IsActive]=1;
                IF @IssuerName IS NULL OR @ReceiverName IS NULL THROW 50005, 'Issuer and receiver must be active users.', 1;

                SELECT @FiscalYearId=fiscalYear.[Id], @RaPrefix=setting.[RaPrefix], @PaPrefix=setting.[PaPrefix],
                       @NextRaNumber=setting.[NextRaNumber], @NextPaNumber=setting.[NextPaNumber]
                FROM [dbo].[FiscalYear] fiscalYear WITH (UPDLOCK,HOLDLOCK)
                JOIN [dbo].[FiscalYearSetting] setting WITH (UPDLOCK,HOLDLOCK) ON setting.[FiscalYearId]=fiscalYear.[Id]
                WHERE fiscalYear.[IsActive]=1 AND fiscalYear.[IsClosed]=0;
                SET @RaValue=TRY_CONVERT(bigint,@NextRaNumber);
                IF @FiscalYearId IS NULL OR @RaValue IS NULL OR @RaValue<0 OR @RaValue=9223372036854775807
                    THROW 50006, 'Active fiscal-year risk-assessment numbering is not configured correctly.', 1;
                SET @RiskAssessmentNumber=CONCAT(@RaPrefix,@NextRaNumber);
                IF LEN(@RiskAssessmentNumber)>50 OR EXISTS(SELECT 1 FROM [dbo].[RiskAssessment] WHERE [RiskAssessmentNumber]=@RiskAssessmentNumber)
                    THROW 50006, 'The next risk-assessment number is invalid or already used.', 1;

                INSERT [dbo].[RiskAssessment] ([RiskAssessmentNumber],[IssueDate],[PermitIssuerUserId],[PermitReceiverUserId],
                    [AreaResponsibleName],[LocationOfWork],[DescriptionOfWork],[SpecialInstructions],[OtherEquipmentsPPE],
                    [OtherProtectionMeasures],[PlannedStartDateTime],[PlannedEndDateTime],[RiskAssessmentStatusListItemId],
                    [CreatedBy],[ModifiedBy],[CreatedAtUtc],[UpdatedAtUtc])
                VALUES (@RiskAssessmentNumber,@IssueDate,@PermitIssuerUserId,@PermitReceiverUserId,@AreaResponsibleName,
                    @LocationOfWork,@DescriptionOfWork,@SpecialInstructions,@OtherEquipmentsPPE,@OtherProtectionMeasures,
                    @PlannedStartDateTime,@PlannedEndDateTime,@DraftStatusId,@CreatedBy,NULL,@Now,@Now);
                SET @RiskAssessmentId=CONVERT(int,SCOPE_IDENTITY());
                INSERT [dbo].[RiskAssessmentAdditionalPPE] ([RiskAssessmentId],[AdditionalProtectiveMeasuresListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @AdditionalPpe;
                INSERT [dbo].[RiskAssessmentHazardCategories] ([RiskAssessmentId],[HazardCategoriesListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @HazardCategories;
                INSERT [dbo].[RiskAssessmentPPE] ([RiskAssessmentId],[SpecialPermitListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @PersonalProtectiveEquipment;
                INSERT [dbo].[RiskAssessmentSpecialPermit] ([RiskAssessmentId],[SpecialPermitListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @SpecialPermits;

                DECLARE @NewRa nvarchar(50)=CONVERT(nvarchar(50),@RaValue+1);
                IF LEN(@NewRa)<LEN(@NextRaNumber) SET @NewRa=REPLICATE(N'0',LEN(@NextRaNumber)-LEN(@NewRa))+@NewRa;
                UPDATE [dbo].[FiscalYearSetting] SET [NextRaNumber]=@NewRa,[UpdatedAtUtc]=@Now,[UpdatedByUserId]=@CreatedBy WHERE [FiscalYearId]=@FiscalYearId;

                IF @PermitCount>0
                BEGIN
                    SET @PaValue=TRY_CONVERT(bigint,@NextPaNumber);
                    IF @PaValue IS NULL OR @PaValue<0 OR @PaValue>9223372036854775807-@PermitCount
                        THROW 50007, 'Active fiscal-year permit numbering is not configured correctly.', 1;
                    ;WITH selected AS
                    (
                        SELECT [ListItemId],ROW_NUMBER() OVER(ORDER BY [ListItemId])-1 AS offsetValue
                        FROM @SpecialPermits WHERE [IsSelected]=1
                    )
                    INSERT [dbo].[PermitApplication] ([RiskAssessmentId],[PermitNumber],[IssueDate],[PermitIssuerName],
                        [PermitReceiverName],[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],
                        [PermitTypeListItemId],[PermitStatusListItemId],[CreatedByUserId],[CreatedAtUtc])
                    SELECT @RiskAssessmentId,CONCAT(@PaPrefix,
                        CASE WHEN LEN(CONVERT(nvarchar(50),@PaValue+offsetValue))<LEN(@NextPaNumber)
                             THEN REPLICATE(N'0',LEN(@NextPaNumber)-LEN(CONVERT(nvarchar(50),@PaValue+offsetValue))) ELSE N'' END,
                        CONVERT(nvarchar(50),@PaValue+offsetValue)),@IssueDate,@IssuerName,@ReceiverName,@RiskAssessmentNumber,
                        @LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,[ListItemId],@PermitDraftStatusId,@CreatedBy,@Now
                    FROM selected;
                    IF EXISTS(SELECT 1 FROM [dbo].[PermitApplication] WHERE [RiskAssessmentId]=@RiskAssessmentId AND LEN([PermitNumber])>50)
                        THROW 50007, 'The generated permit number exceeds 50 characters.', 1;
                    DECLARE @NewPa nvarchar(50)=CONVERT(nvarchar(50),@PaValue+@PermitCount);
                    IF LEN(@NewPa)<LEN(@NextPaNumber) SET @NewPa=REPLICATE(N'0',LEN(@NextPaNumber)-LEN(@NewPa))+@NewPa;
                    UPDATE [dbo].[FiscalYearSetting] SET [NextPaNumber]=@NewPa,[UpdatedAtUtc]=@Now,[UpdatedByUserId]=@CreatedBy WHERE [FiscalYearId]=@FiscalYearId;
                END;

                INSERT [dbo].[AuditLog] ([EntityName],[Action],[EntityKey],[ChangedColumns],[NewValues],[ChangedByUserId],[ChangedAtUtc])
                VALUES (N'RiskAssessment',N'INSERT',CONCAT(N'{"Id":',@RiskAssessmentId,N'}'),N'RiskAssessmentNumber,PermitIssuerUserId,PermitReceiverUserId',
                        CONCAT(N'{"RiskAssessmentNumber":"',STRING_ESCAPE(@RiskAssessmentNumber,'json'),N'"}'),@CreatedBy,@Now);
                COMMIT;
            END TRY
            BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
            SELECT @RiskAssessmentId [RiskAssessmentId],@RiskAssessmentNumber [RiskAssessmentNumber],
                   @DraftStatusId [RiskAssessmentStatusListItemId],N'Draft' [Status],@Now [UpdatedAtUtc];
        END;
        """;

    internal static string CreateUpdateProcedure() => """
        CREATE OR ALTER PROCEDURE [dbo].[SpRiskAssessmentUpd]
            @RiskAssessmentId int, @RequireCreatedBy bit=0,
            @IssueDate date, @PermitIssuerUserId int, @PermitReceiverUserId int,
            @AreaResponsibleName nvarchar(100), @LocationOfWork nvarchar(255),
            @DescriptionOfWork nvarchar(max)=NULL, @SpecialInstructions nvarchar(max)=NULL,
            @OtherEquipmentsPPE nvarchar(500)=NULL, @OtherProtectionMeasures nvarchar(500)=NULL,
            @PlannedStartDateTime datetime2(0)=NULL, @PlannedEndDateTime datetime2(0)=NULL,
            @ModifiedBy int,
            @AdditionalPpe [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @HazardCategories [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @PersonalProtectiveEquipment [dbo].[RiskAssessmentSelectionTableType] READONLY,
            @SpecialPermits [dbo].[RiskAssessmentSelectionTableType] READONLY
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            DECLARE @CurrentStatusId int,@CreatedBy int,@StatusName nvarchar(100),@RiskAssessmentNumber nvarchar(50),
                    @Now datetime2(0)=SYSUTCDATETIME(),@IssuerName nvarchar(200),@ReceiverName nvarchar(200),
                    @PermitDraftStatusId int,@FiscalYearId int,@PaPrefix nvarchar(50),@NextPaNumber nvarchar(50),
                    @PaValue bigint,@PermitCount int;
            SELECT @PermitDraftStatusId=item.[ListItemId] FROM [dbo].[ListItem] item
            JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=item.[ListItemCategoryId]
            WHERE category.[Code]=N'PERMIT_STATUS' AND item.[SystemName]=N'PERMIT_DRAFT';
            IF @PermitDraftStatusId IS NULL THROW 50004, 'The Draft permit status is not configured.', 1;

            BEGIN TRY
                BEGIN TRANSACTION;
                SELECT @CurrentStatusId=riskAssessment.[RiskAssessmentStatusListItemId],@CreatedBy=riskAssessment.[CreatedBy],
                       @RiskAssessmentNumber=riskAssessment.[RiskAssessmentNumber],@StatusName=statusItem.[ItemName]
                FROM [dbo].[RiskAssessment] riskAssessment WITH (UPDLOCK,HOLDLOCK)
                JOIN [dbo].[ListItem] statusItem ON statusItem.[ListItemId]=riskAssessment.[RiskAssessmentStatusListItemId]
                JOIN [dbo].[ListItemCategory] category ON category.[ListItemCategoryId]=statusItem.[ListItemCategoryId]
                WHERE riskAssessment.[Id]=@RiskAssessmentId AND category.[Code]=N'RISK_ASSESSMENT_STATUS'
                  AND statusItem.[SystemName] IN (N'DRAFT',N'REJECTED');
                IF @RiskAssessmentNumber IS NULL
                BEGIN
                    IF EXISTS(SELECT 1 FROM [dbo].[RiskAssessment] WHERE [Id]=@RiskAssessmentId)
                        THROW 50002, 'Only Draft or Rejected risk assessments can be edited.', 1;
                    THROW 50001, 'Risk assessment was not found.', 1;
                END;
                IF @RequireCreatedBy=1 AND @CreatedBy<>@ModifiedBy THROW 50002, 'This draft belongs to another user.', 1;
                SELECT @IssuerName=COALESCE([DisplayName],[UserName]) FROM [dbo].[Users] WHERE [Id]=@PermitIssuerUserId AND [IsActive]=1;
                SELECT @ReceiverName=COALESCE([DisplayName],[UserName]) FROM [dbo].[Users] WHERE [Id]=@PermitReceiverUserId AND [IsActive]=1;
                IF @IssuerName IS NULL OR @ReceiverName IS NULL THROW 50005, 'Issuer and receiver must be active users.', 1;

                IF EXISTS
                (
                    SELECT 1 FROM [dbo].[PermitApplication] permitApplication
                    JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
                    WHERE permitApplication.[RiskAssessmentId]=@RiskAssessmentId AND permitStatus.[SystemName]<>N'PERMIT_DRAFT'
                      AND NOT EXISTS(SELECT 1 FROM @SpecialPermits selected WHERE selected.[ListItemId]=permitApplication.[PermitTypeListItemId] AND selected.[IsSelected]=1)
                ) THROW 50002, 'A non-Draft permit cannot be removed from this risk assessment.', 1;

                UPDATE [dbo].[RiskAssessment] SET [IssueDate]=@IssueDate,[PermitIssuerUserId]=@PermitIssuerUserId,
                    [PermitReceiverUserId]=@PermitReceiverUserId,[AreaResponsibleName]=@AreaResponsibleName,
                    [LocationOfWork]=@LocationOfWork,[DescriptionOfWork]=@DescriptionOfWork,[SpecialInstructions]=@SpecialInstructions,
                    [OtherEquipmentsPPE]=@OtherEquipmentsPPE,[OtherProtectionMeasures]=@OtherProtectionMeasures,
                    [PlannedStartDateTime]=@PlannedStartDateTime,[PlannedEndDateTime]=@PlannedEndDateTime,
                    [ModifiedBy]=@ModifiedBy,[UpdatedAtUtc]=@Now WHERE [Id]=@RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentAdditionalPPE] WHERE [RiskAssessmentId]=@RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentHazardCategories] WHERE [RiskAssessmentId]=@RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentPPE] WHERE [RiskAssessmentId]=@RiskAssessmentId;
                DELETE FROM [dbo].[RiskAssessmentSpecialPermit] WHERE [RiskAssessmentId]=@RiskAssessmentId;
                INSERT [dbo].[RiskAssessmentAdditionalPPE] ([RiskAssessmentId],[AdditionalProtectiveMeasuresListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @AdditionalPpe;
                INSERT [dbo].[RiskAssessmentHazardCategories] ([RiskAssessmentId],[HazardCategoriesListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @HazardCategories;
                INSERT [dbo].[RiskAssessmentPPE] ([RiskAssessmentId],[SpecialPermitListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @PersonalProtectiveEquipment;
                INSERT [dbo].[RiskAssessmentSpecialPermit] ([RiskAssessmentId],[SpecialPermitListItemId],[IsSelected])
                    SELECT @RiskAssessmentId,[ListItemId],[IsSelected] FROM @SpecialPermits;

                DELETE permitApplication FROM [dbo].[PermitApplication] permitApplication
                JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
                WHERE permitApplication.[RiskAssessmentId]=@RiskAssessmentId AND permitStatus.[SystemName]=N'PERMIT_DRAFT'
                  AND NOT EXISTS(SELECT 1 FROM @SpecialPermits selected WHERE selected.[ListItemId]=permitApplication.[PermitTypeListItemId] AND selected.[IsSelected]=1);
                UPDATE permitApplication SET [IssueDate]=@IssueDate,[PermitIssuerName]=@IssuerName,[PermitReceiverName]=@ReceiverName,
                    [RiskAssessmentNumber]=@RiskAssessmentNumber,[WorkLocation]=@LocationOfWork,
                    [WorkDescription]=COALESCE(@DescriptionOfWork,N''),[SpecialInstructions]=@SpecialInstructions,
                    [UpdatedByUserId]=@ModifiedBy,[UpdatedAtUtc]=@Now
                FROM [dbo].[PermitApplication] permitApplication
                JOIN [dbo].[ListItem] permitStatus ON permitStatus.[ListItemId]=permitApplication.[PermitStatusListItemId]
                WHERE permitApplication.[RiskAssessmentId]=@RiskAssessmentId AND permitStatus.[SystemName]=N'PERMIT_DRAFT';

                DECLARE @Missing TABLE ([ListItemId] int PRIMARY KEY,[OffsetValue] int);
                INSERT @Missing ([ListItemId],[OffsetValue])
                SELECT selected.[ListItemId],CONVERT(int,ROW_NUMBER() OVER(ORDER BY selected.[ListItemId])-1)
                FROM @SpecialPermits selected WHERE selected.[IsSelected]=1
                  AND NOT EXISTS(SELECT 1 FROM [dbo].[PermitApplication] permitApplication
                                 WHERE permitApplication.[RiskAssessmentId]=@RiskAssessmentId AND permitApplication.[PermitTypeListItemId]=selected.[ListItemId]);
                SELECT @PermitCount=COUNT(*) FROM @Missing;
                IF @PermitCount>0
                BEGIN
                    SELECT @FiscalYearId=fiscalYear.[Id],@PaPrefix=setting.[PaPrefix],@NextPaNumber=setting.[NextPaNumber]
                    FROM [dbo].[FiscalYear] fiscalYear WITH (UPDLOCK,HOLDLOCK)
                    JOIN [dbo].[FiscalYearSetting] setting WITH (UPDLOCK,HOLDLOCK) ON setting.[FiscalYearId]=fiscalYear.[Id]
                    WHERE fiscalYear.[IsActive]=1 AND fiscalYear.[IsClosed]=0;
                    SET @PaValue=TRY_CONVERT(bigint,@NextPaNumber);
                    IF @FiscalYearId IS NULL OR @PaValue IS NULL OR @PaValue<0 OR @PaValue>9223372036854775807-@PermitCount
                        THROW 50007, 'Active fiscal-year permit numbering is not configured correctly.', 1;
                    INSERT [dbo].[PermitApplication] ([RiskAssessmentId],[PermitNumber],[IssueDate],[PermitIssuerName],
                        [PermitReceiverName],[RiskAssessmentNumber],[WorkLocation],[WorkDescription],[SpecialInstructions],
                        [PermitTypeListItemId],[PermitStatusListItemId],[CreatedByUserId],[CreatedAtUtc])
                    SELECT @RiskAssessmentId,CONCAT(@PaPrefix,
                        CASE WHEN LEN(CONVERT(nvarchar(50),@PaValue+[OffsetValue]))<LEN(@NextPaNumber)
                             THEN REPLICATE(N'0',LEN(@NextPaNumber)-LEN(CONVERT(nvarchar(50),@PaValue+[OffsetValue]))) ELSE N'' END,
                        CONVERT(nvarchar(50),@PaValue+[OffsetValue])),@IssueDate,@IssuerName,@ReceiverName,@RiskAssessmentNumber,
                        @LocationOfWork,COALESCE(@DescriptionOfWork,N''),@SpecialInstructions,[ListItemId],@PermitDraftStatusId,@ModifiedBy,@Now FROM @Missing;
                    IF EXISTS(SELECT 1 FROM [dbo].[PermitApplication] WHERE [RiskAssessmentId]=@RiskAssessmentId AND LEN([PermitNumber])>50)
                        THROW 50007, 'The generated permit number exceeds 50 characters.', 1;
                    DECLARE @NewPa nvarchar(50)=CONVERT(nvarchar(50),@PaValue+@PermitCount);
                    IF LEN(@NewPa)<LEN(@NextPaNumber) SET @NewPa=REPLICATE(N'0',LEN(@NextPaNumber)-LEN(@NewPa))+@NewPa;
                    UPDATE [dbo].[FiscalYearSetting] SET [NextPaNumber]=@NewPa,[UpdatedAtUtc]=@Now,[UpdatedByUserId]=@ModifiedBy WHERE [FiscalYearId]=@FiscalYearId;
                END;
                INSERT [dbo].[AuditLog] ([EntityName],[Action],[EntityKey],[ChangedColumns],[ChangedByUserId],[ChangedAtUtc])
                VALUES (N'RiskAssessment',N'UPDATE',CONCAT(N'{"Id":',@RiskAssessmentId,N'}'),
                        N'IssueDate,PermitIssuerUserId,PermitReceiverUserId,AreaResponsibleName,LocationOfWork,Selections',@ModifiedBy,@Now);
                COMMIT;
            END TRY
            BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
            SELECT @RiskAssessmentId [RiskAssessmentId],@RiskAssessmentNumber [RiskAssessmentNumber],
                   @CurrentStatusId [RiskAssessmentStatusListItemId],@StatusName [Status],@Now [UpdatedAtUtc];
        END;
        """;

    private static string ReplaceProcedureReferences(string oldName, string newName) => $$"""
        DECLARE @ProcedureDefinition nvarchar(max);
        DECLARE @OldCamelName nvarchar(128)=LOWER(LEFT(N'{{oldName}}',1))+SUBSTRING(N'{{oldName}}',2,128);
        DECLARE @NewCamelName nvarchar(128)=LOWER(LEFT(N'{{newName}}',1))+SUBSTRING(N'{{newName}}',2,128);
        DECLARE procedure_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT module.[definition]
            FROM sys.sql_modules module
            INNER JOIN sys.objects procedureObject ON procedureObject.[object_id]=module.[object_id]
            WHERE procedureObject.[type]=N'P' AND module.[definition] LIKE N'%{{oldName}}%';
        OPEN procedure_cursor;
        FETCH NEXT FROM procedure_cursor INTO @ProcedureDefinition;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @ProcedureDefinition=REPLACE(@ProcedureDefinition,N'{{oldName}}',N'{{newName}}');
            SET @ProcedureDefinition=REPLACE(@ProcedureDefinition,@OldCamelName,@NewCamelName);
            DECLARE @ProcedureKeywordPosition int=CHARINDEX(N'PROCEDURE',UPPER(@ProcedureDefinition));
            IF @ProcedureKeywordPosition=0
                THROW 50020, 'A stored procedure dependency could not be rewritten safely.', 1;
            SET @ProcedureDefinition=N'ALTER PROCEDURE'+SUBSTRING(
                @ProcedureDefinition,
                @ProcedureKeywordPosition+LEN(N'PROCEDURE'),
                LEN(@ProcedureDefinition));
            EXEC sys.sp_executesql @ProcedureDefinition;
            FETCH NEXT FROM procedure_cursor INTO @ProcedureDefinition;
        END;
        CLOSE procedure_cursor;
        DEALLOCATE procedure_cursor;
        """;
}
