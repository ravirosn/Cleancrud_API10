using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanCrud.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819141000_EnforceSingleActiveApprovalWorkflowPerPermitType")]
public partial class EnforceSingleActiveApprovalWorkflowPerPermitType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApprovalWorkflow_PermitTypeListItemId",
            schema: "dbo",
            table: "ApprovalWorkflow");

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalWorkflow_PermitTypeListItemId",
            schema: "dbo",
            table: "ApprovalWorkflow",
            column: "PermitTypeListItemId",
            unique: true,
            filter: "[IsActive] = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApprovalWorkflow_PermitTypeListItemId",
            schema: "dbo",
            table: "ApprovalWorkflow");

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalWorkflow_PermitTypeListItemId",
            schema: "dbo",
            table: "ApprovalWorkflow",
            column: "PermitTypeListItemId",
            unique: true);
    }
}
