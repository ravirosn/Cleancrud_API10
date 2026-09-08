using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909090000_FixRiskAssessmentPermitDraftStatus")]
public sealed class FixRiskAssessmentPermitDraftStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ConfigureRiskAssessmentNumberingAndUsers.CreateInsertProcedure());
        migrationBuilder.Sql(ConfigureRiskAssessmentNumberingAndUsers.CreateUpdateProcedure());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The previous definitions used the wrong permit Draft code and must not be restored.
    }
}
