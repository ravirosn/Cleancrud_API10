using Apcloudpms.Infrastructure.Migrations.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

public partial class AddPermitApplicationEditorProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreateUpdateProcedure());
        migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreateHotWorkUpdateProcedure());
        migrationBuilder.Sql(PermitApplicationEditorProcedureSql.CreatePreviewProcedure());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpPermitApplicationPreviewGet];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpPermitApplicationHotWorkUpd];");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SpPermitApplicationUpd];");
    }
}
