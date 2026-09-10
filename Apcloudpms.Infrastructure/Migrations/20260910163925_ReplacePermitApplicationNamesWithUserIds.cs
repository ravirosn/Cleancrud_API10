using Apcloudpms.Infrastructure.Migrations.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

public partial class ReplacePermitApplicationNamesWithUserIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "PermitIssuerId", schema: "dbo", table: "PermitApplication",
            type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "PermitReceiverId", schema: "dbo", table: "PermitApplication",
            type: "int", nullable: true);

        migrationBuilder.Sql("""
            UPDATE permitApplication
            SET [PermitIssuerId] = COALESCE(riskAssessment.[PermitIssuerUserId], issuerMatch.[UserId]),
                [PermitReceiverId] = COALESCE(riskAssessment.[PermitReceiverUserId], receiverMatch.[UserId])
            FROM [dbo].[PermitApplication] permitApplication
            LEFT JOIN [dbo].[RiskAssessment] riskAssessment ON riskAssessment.[Id] = permitApplication.[RiskAssessmentId]
            OUTER APPLY
            (
                SELECT MIN([Id]) AS [UserId], COUNT_BIG(*) AS [MatchCount]
                FROM [dbo].[Users]
                WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
                   OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
            ) issuerMatch
            OUTER APPLY
            (
                SELECT MIN([Id]) AS [UserId], COUNT_BIG(*) AS [MatchCount]
                FROM [dbo].[Users]
                WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
                   OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
            ) receiverMatch;

            IF EXISTS
            (
                SELECT 1
                FROM [dbo].[PermitApplication] permitApplication
                LEFT JOIN [dbo].[RiskAssessment] riskAssessment ON riskAssessment.[Id] = permitApplication.[RiskAssessmentId]
                OUTER APPLY
                (
                    SELECT COUNT_BIG(*) AS [MatchCount] FROM [dbo].[Users]
                    WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
                       OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitIssuerName]))
                ) issuerMatch
                OUTER APPLY
                (
                    SELECT COUNT_BIG(*) AS [MatchCount] FROM [dbo].[Users]
                    WHERE LTRIM(RTRIM(COALESCE([DisplayName], [UserName]))) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
                       OR LTRIM(RTRIM([UserName])) = LTRIM(RTRIM(permitApplication.[PermitReceiverName]))
                ) receiverMatch
                WHERE permitApplication.[PermitIssuerId] IS NULL
                   OR permitApplication.[PermitReceiverId] IS NULL
                   OR (riskAssessment.[Id] IS NULL AND (issuerMatch.[MatchCount] <> 1 OR receiverMatch.[MatchCount] <> 1))
            )
                THROW 50021, 'PermitApplication issuer/receiver names could not be mapped uniquely to users.', 1;
            """);

        migrationBuilder.Sql("ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitIssuerId] int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitReceiverId] int NOT NULL;");

        migrationBuilder.Sql(PermitApplicationUserProcedureSql.RewriteRiskAssessmentProcedures(true));
        migrationBuilder.DropColumn(name: "PermitIssuerName", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "PermitReceiverName", schema: "dbo", table: "PermitApplication");

        migrationBuilder.CreateIndex(name: "IX_PermitApplication_PermitIssuerId", schema: "dbo",
            table: "PermitApplication", column: "PermitIssuerId");
        migrationBuilder.CreateIndex(name: "IX_PermitApplication_PermitReceiverId", schema: "dbo",
            table: "PermitApplication", column: "PermitReceiverId");
        migrationBuilder.AddForeignKey(name: "FK_PermitApplication_Users_PermitIssuerId", schema: "dbo",
            table: "PermitApplication", column: "PermitIssuerId", principalSchema: "dbo", principalTable: "Users",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_PermitApplication_Users_PermitReceiverId", schema: "dbo",
            table: "PermitApplication", column: "PermitReceiverId", principalSchema: "dbo", principalTable: "Users",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(PermitApplicationUserProcedureSql.CreatePermitApplicationsGet(true));
        migrationBuilder.Sql(PermitApplicationUserProcedureSql.CreatePermitApprovalHistoryGet(true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_PermitApplication_Users_PermitIssuerId", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropForeignKey(name: "FK_PermitApplication_Users_PermitReceiverId", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropIndex(name: "IX_PermitApplication_PermitIssuerId", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropIndex(name: "IX_PermitApplication_PermitReceiverId", schema: "dbo", table: "PermitApplication");

        migrationBuilder.AddColumn<string>(name: "PermitIssuerName", schema: "dbo", table: "PermitApplication", type: "nvarchar(200)",
            maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PermitReceiverName", schema: "dbo", table: "PermitApplication", type: "nvarchar(200)",
            maxLength: 200, nullable: true);
        migrationBuilder.Sql("""
            UPDATE permitApplication
            SET [PermitIssuerName] = COALESCE(issuer.[DisplayName], issuer.[UserName]),
                [PermitReceiverName] = COALESCE(receiver.[DisplayName], receiver.[UserName])
            FROM [dbo].[PermitApplication] permitApplication
            INNER JOIN [dbo].[Users] issuer ON issuer.[Id] = permitApplication.[PermitIssuerId]
            INNER JOIN [dbo].[Users] receiver ON receiver.[Id] = permitApplication.[PermitReceiverId];
            """);
        migrationBuilder.Sql("ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitIssuerName] nvarchar(200) NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE [dbo].[PermitApplication] ALTER COLUMN [PermitReceiverName] nvarchar(200) NOT NULL;");

        migrationBuilder.Sql(PermitApplicationUserProcedureSql.RewriteRiskAssessmentProcedures(false));
        migrationBuilder.Sql(PermitApplicationUserProcedureSql.CreatePermitApplicationsGet(false));
        migrationBuilder.Sql(PermitApplicationUserProcedureSql.CreatePermitApprovalHistoryGet(false));
        migrationBuilder.DropColumn(name: "PermitIssuerId", schema: "dbo", table: "PermitApplication");
        migrationBuilder.DropColumn(name: "PermitReceiverId", schema: "dbo", table: "PermitApplication");
    }
}
