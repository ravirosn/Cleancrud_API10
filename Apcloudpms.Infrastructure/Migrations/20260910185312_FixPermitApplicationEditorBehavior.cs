using Apcloudpms.Infrastructure.Migrations.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

public partial class FixPermitApplicationEditorBehavior : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreateUpdateProcedure());
        migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreatePreviewProcedure());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // These procedure corrections are intentionally retained on rollback.
    }
}
