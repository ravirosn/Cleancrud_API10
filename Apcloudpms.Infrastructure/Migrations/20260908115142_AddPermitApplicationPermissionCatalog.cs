using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apcloudpms.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260908115142_AddPermitApplicationPermissionCatalog")]
public partial class AddPermitApplicationPermissionCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        MERGE dbo.PermissionPolicy AS target
        USING (VALUES
          (N'PermitDashboard.View',N'View permit dashboard',N'Open the permit dashboard.',N'PERMIT',N'PermitDashboard',N'Index'),
          (N'PermitDashboard.Export',N'Export permit dashboard',N'Export permit dashboard reports.',N'PERMIT',N'PermitDashboard',N'Index'),
          (N'PermitApplication.View',N'View permit applications',N'View and search permit applications.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Create',N'Create permit applications',N'Create permit applications.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Edit',N'Edit permit applications',N'Edit permit applications while their workflow state permits changes.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Finalize',N'Finalize permit applications',N'Finalize and submit permit applications.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Complete',N'Complete permit applications',N'Mark permit work as completed.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Cancel',N'Cancel permit applications',N'Cancel or close permit applications.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApplication.Print',N'Print permit applications',N'Print permit application records.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'RiskAssessment.View',N'View risk assessments',N'View risk assessments and their related permit applications.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'RiskAssessment.Create',N'Create risk assessments',N'Create risk assessments.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'RiskAssessment.Edit',N'Edit risk assessments',N'Edit draft risk assessments.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'RiskAssessment.Submit',N'Submit risk assessments',N'Submit risk assessments into the approval workflow.',N'PERMIT',N'PermitApplications',N'Index'),
          (N'PermitApproval.View',N'View permit approvals',N'View pending and historical permit approvals.',N'PERMIT',N'PermitApprovals',N'Index'),
          (N'PermitApproval.Decide',N'Decide permit approvals',N'Approve or reject assigned permit approvals.',N'PERMIT',N'PermitApprovals',N'Index'),
          (N'PermitApproval.ViewAssignments',N'View approval assignments',N'View pending approval assignments across approvers.',N'PERMIT',N'PermitApprovals',N'Index'),
          (N'PermitApproval.ManageAlternateApprovers',N'Manage alternate approvers',N'Assign alternate users for permit approvals.',N'PERMIT',N'PermitApprovals',N'Index')
        ) AS source(Code,Name,Description,ModuleCode,MenuController,MenuAction)
        ON target.Code=source.Code
        WHEN MATCHED THEN UPDATE SET Name=source.Name,Description=source.Description,
          ModuleCode=source.ModuleCode,MenuController=source.MenuController,MenuAction=source.MenuAction,
          RequiresMenuAccess=1,IsActive=1,UpdatedAtUtc=SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT(Code,Name,Description,ModuleCode,MenuController,MenuAction,RequiresMenuAccess,IsActive,CreatedAtUtc)
          VALUES(source.Code,source.Name,source.Description,source.ModuleCode,source.MenuController,source.MenuAction,1,1,SYSUTCDATETIME());

        INSERT dbo.RolePermission(RoleId,PermissionPolicyId,IsActive,AssignedAtUtc,AssignedBy)
        SELECT role.Id,policy.Id,1,SYSUTCDATETIME(),N'System permission catalog migration'
        FROM dbo.Role role CROSS JOIN dbo.PermissionPolicy policy
        WHERE role.NormalizedName=N'ADMIN' AND role.IsActive=1 AND policy.Code IN (
          N'PermitDashboard.View',N'PermitDashboard.Export',N'PermitApplication.View',N'PermitApplication.Create',N'PermitApplication.Edit',
          N'PermitApplication.Finalize',N'PermitApplication.Complete',N'PermitApplication.Cancel',N'PermitApplication.Print',
          N'RiskAssessment.View',N'RiskAssessment.Create',N'RiskAssessment.Edit',N'RiskAssessment.Submit',N'PermitApproval.View',
          N'PermitApproval.Decide',N'PermitApproval.ViewAssignments',N'PermitApproval.ManageAlternateApprovers')
          AND NOT EXISTS(SELECT 1 FROM dbo.RolePermission assignment
            WHERE assignment.RoleId=role.Id AND assignment.PermissionPolicyId=policy.Id);

        UPDATE assignment SET IsActive=1,ModifiedAtUtc=SYSUTCDATETIME(),ModifiedBy=N'System permission catalog migration'
        FROM dbo.RolePermission assignment
        JOIN dbo.Role role ON role.Id=assignment.RoleId
        JOIN dbo.PermissionPolicy policy ON policy.Id=assignment.PermissionPolicyId
        WHERE role.NormalizedName=N'ADMIN' AND role.IsActive=1 AND policy.Code IN (
          N'PermitDashboard.View',N'PermitDashboard.Export',N'PermitApplication.View',N'PermitApplication.Create',N'PermitApplication.Edit',
          N'PermitApplication.Finalize',N'PermitApplication.Complete',N'PermitApplication.Cancel',N'PermitApplication.Print',
          N'RiskAssessment.View',N'RiskAssessment.Create',N'RiskAssessment.Edit',N'RiskAssessment.Submit',N'PermitApproval.View',
          N'PermitApproval.Decide',N'PermitApproval.ViewAssignments',N'PermitApproval.ManageAlternateApprovers') AND assignment.IsActive=0;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DELETE assignment FROM dbo.RolePermission assignment
        JOIN dbo.PermissionPolicy policy ON policy.Id=assignment.PermissionPolicyId
        WHERE policy.Code IN (
          N'PermitDashboard.View',N'PermitDashboard.Export',N'PermitApplication.View',N'PermitApplication.Create',N'PermitApplication.Edit',
          N'PermitApplication.Finalize',N'PermitApplication.Complete',N'PermitApplication.Cancel',N'PermitApplication.Print',
          N'RiskAssessment.View',N'RiskAssessment.Create',N'RiskAssessment.Edit',N'RiskAssessment.Submit',N'PermitApproval.View',
          N'PermitApproval.Decide',N'PermitApproval.ViewAssignments',N'PermitApproval.ManageAlternateApprovers');
        DELETE FROM dbo.PermissionPolicy WHERE Code IN (
          N'PermitDashboard.View',N'PermitDashboard.Export',N'PermitApplication.View',N'PermitApplication.Create',N'PermitApplication.Edit',
          N'PermitApplication.Finalize',N'PermitApplication.Complete',N'PermitApplication.Cancel',N'PermitApplication.Print',
          N'RiskAssessment.View',N'RiskAssessment.Create',N'RiskAssessment.Edit',N'RiskAssessment.Submit',N'PermitApproval.View',
          N'PermitApproval.Decide',N'PermitApproval.ViewAssignments',N'PermitApproval.ManageAlternateApprovers');
        """);
}
