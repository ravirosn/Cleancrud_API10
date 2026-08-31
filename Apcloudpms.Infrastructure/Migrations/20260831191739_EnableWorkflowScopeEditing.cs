using Microsoft.EntityFrameworkCore.Migrations;

using Apcloudpms.Infrastructure.Migrations.Sql;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnableWorkflowScopeEditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WorkflowSetupProcedureSql.EditableEditProcedure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WorkflowSetupProcedureSql.ImmutableEditProcedure);
        }
    }
}
