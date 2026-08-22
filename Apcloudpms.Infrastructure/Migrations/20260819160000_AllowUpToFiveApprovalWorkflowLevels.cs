using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819160000_AllowUpToFiveApprovalWorkflowLevels")]
public partial class AllowUpToFiveApprovalWorkflowLevels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ApprovalWorkflowLevel_LevelNumber",
            schema: "dbo",
            table: "ApprovalWorkflowLevel");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PermitApproval_LevelNumber",
            schema: "dbo",
            table: "PermitApproval");

        migrationBuilder.AddCheckConstraint(
            name: "CK_ApprovalWorkflowLevel_LevelNumber",
            schema: "dbo",
            table: "ApprovalWorkflowLevel",
            sql: "[LevelNumber] BETWEEN 1 AND 5");

        migrationBuilder.AddCheckConstraint(
            name: "CK_PermitApproval_LevelNumber",
            schema: "dbo",
            table: "PermitApproval",
            sql: "[LevelNumber] BETWEEN 1 AND 5");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ApprovalWorkflowLevel_LevelNumber",
            schema: "dbo",
            table: "ApprovalWorkflowLevel");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PermitApproval_LevelNumber",
            schema: "dbo",
            table: "PermitApproval");

        migrationBuilder.AddCheckConstraint(
            name: "CK_ApprovalWorkflowLevel_LevelNumber",
            schema: "dbo",
            table: "ApprovalWorkflowLevel",
            sql: "[LevelNumber] BETWEEN 1 AND 3");

        migrationBuilder.AddCheckConstraint(
            name: "CK_PermitApproval_LevelNumber",
            schema: "dbo",
            table: "PermitApproval",
            sql: "[LevelNumber] BETWEEN 1 AND 3");
    }
}
