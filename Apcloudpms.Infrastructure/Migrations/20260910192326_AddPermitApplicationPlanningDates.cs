using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Apcloudpms.Infrastructure.Migrations.Sql;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitApplicationPlanningDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndDateTime",
                schema: "dbo",
                table: "PermitApplication",
                type: "datetime2(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartDateTime",
                schema: "dbo",
                table: "PermitApplication",
                type: "datetime2(0)",
                precision: 0,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE permitApplication
                SET permitApplication.[PlannedStartDateTime]=riskAssessment.[PlannedStartDateTime],
                    permitApplication.[PlannedEndDateTime]=riskAssessment.[PlannedEndDateTime]
                FROM [dbo].[PermitApplication] permitApplication
                INNER JOIN [dbo].[RiskAssessment] riskAssessment
                    ON riskAssessment.[Id]=permitApplication.[RiskAssessmentId]
                WHERE permitApplication.[PlannedStartDateTime] IS NULL
                  AND permitApplication.[PlannedEndDateTime] IS NULL;
                """);

            migrationBuilder.Sql(PermitApplicationPlanningProcedureSql.CreateUpdateProcedure());
            migrationBuilder.Sql(PermitApplicationPlanningProcedureSql.CreatePreviewProcedure());
            migrationBuilder.Sql(PermitApplicationPlanningProcedureSql.RewriteRiskAssessmentProcedures(
                includePlanningDates: true));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PermitApplicationPlanningProcedureSql.RewriteRiskAssessmentProcedures(
                includePlanningDates: false));
            migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreateUpdateProcedure());
            migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreatePreviewProcedure());

            migrationBuilder.DropColumn(
                name: "PlannedEndDateTime",
                schema: "dbo",
                table: "PermitApplication");

            migrationBuilder.DropColumn(
                name: "PlannedStartDateTime",
                schema: "dbo",
                table: "PermitApplication");
        }
    }
}
