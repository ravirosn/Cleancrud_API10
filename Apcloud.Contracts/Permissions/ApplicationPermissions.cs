namespace Apcloud.Contracts.Permissions;

public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Description,
    string ModuleCode,
    string MenuController,
    string MenuAction,
    bool RequiresMenuAccess = true);

/// <summary>
/// Canonical permission codes shared by the Web, API, and database seed migration.
/// Codes are stable identifiers and must not be renamed after they are assigned.
/// </summary>
public static class ApplicationPermissions
{
    public const string OrganizationModule = "ORGANIZATION";
    public const string PermitModule = "PERMIT";
    public const string PortalModule = "PORTAL";

    public static class PermitDashboard
    {
        public const string View = "PermitDashboard.View";
        public const string Export = "PermitDashboard.Export";
    }

    public static class PermitApplications
    {
        public const string View = "PermitApplication.View";
        public const string Create = "PermitApplication.Create";
        public const string Edit = "PermitApplication.Edit";
        public const string Finalize = "PermitApplication.Finalize";
        public const string Complete = "PermitApplication.Complete";
        public const string Cancel = "PermitApplication.Cancel";
        public const string Print = "PermitApplication.Print";
    }

    public static class RiskAssessments
    {
        public const string View = "RiskAssessment.View";
        public const string Create = "RiskAssessment.Create";
        public const string Edit = "RiskAssessment.Edit";
        public const string Submit = "RiskAssessment.Submit";
    }

    public static class PermitApprovals
    {
        public const string View = "PermitApproval.View";
        public const string Decide = "PermitApproval.Decide";
        public const string ViewAssignments = "PermitApproval.ViewAssignments";
        public const string ManageAlternateApprovers = "PermitApproval.ManageAlternateApprovers";
    }

    public static class Organization
    {
        public const string View = "Organization.View";
        public const string Edit = "Organization.Edit";
    }

    public static class OfficeBranches
    {
        public const string View = "OfficeBranch.View";
        public const string Create = "OfficeBranch.Create";
        public const string Edit = "OfficeBranch.Edit";
        public const string Delete = "OfficeBranch.Delete";
    }

    public static class Departments
    {
        public const string View = "Department.View";
        public const string Create = "Department.Create";
        public const string Edit = "Department.Edit";
        public const string Delete = "Department.Delete";
    }

    public static class FiscalYears
    {
        public const string View = "FiscalYear.View";
        public const string Create = "FiscalYear.Create";
        public const string Edit = "FiscalYear.Edit";
        public const string Close = "FiscalYear.Close";
        public const string Delete = "FiscalYear.Delete";
    }

    public static class ListItemCategories
    {
        public const string View = "ListItemCategory.View";
        public const string Create = "ListItemCategory.Create";
        public const string Edit = "ListItemCategory.Edit";
        public const string Delete = "ListItemCategory.Delete";
    }

    public static class ListItems
    {
        public const string View = "ListItem.View";
        public const string Create = "ListItem.Create";
        public const string Edit = "ListItem.Edit";
        public const string Delete = "ListItem.Delete";
    }

    public static class PermitApplicationGuidelines
    {
        public const string View = "PermitApplicationGuideline.View";
        public const string Create = "PermitApplicationGuideline.Create";
        public const string Edit = "PermitApplicationGuideline.Edit";
        public const string Delete = "PermitApplicationGuideline.Delete";
    }

    public static class Roles
    {
        public const string View = "Role.View";
        public const string Create = "Role.Create";
        public const string Edit = "Role.Edit";
        public const string Delete = "Role.Delete";
        public const string AssignUsers = "Role.AssignUsers";
    }

    public static class Modules
    {
        public const string View = "Module.View";
        public const string Create = "Module.Create";
        public const string Edit = "Module.Edit";
        public const string Delete = "Module.Delete";
        public const string Configure = "Module.Configure";
    }

    public static class RoleModuleMenus
    {
        public const string View = "RoleModuleMenu.View";
        public const string Create = "RoleModuleMenu.Create";
        public const string Edit = "RoleModuleMenu.Edit";
        public const string Delete = "RoleModuleMenu.Delete";
    }

    public static class Users
    {
        public const string View = "User.View";
        public const string Create = "User.Create";
        public const string Edit = "User.Edit";
        public const string Delete = "User.Delete";
        public const string AssignRoles = "User.AssignRoles";
    }

    public static class Workflows
    {
        public const string View = "Workflow.View";
        public const string Create = "Workflow.Create";
        public const string Edit = "Workflow.Edit";
        public const string Delete = "Workflow.Delete";
    }

    public static class AuditLogs
    {
        public const string View = "AuditLog.View";
        public const string Export = "AuditLog.Export";
    }

    public static class PermissionAssignments
    {
        public const string View = "PermissionAssignment.View";
        public const string Create = "PermissionAssignment.Create";
        public const string Edit = "PermissionAssignment.Edit";
        public const string Manage = "PermissionAssignment.Manage";
        public const string Delete = "PermissionAssignment.Delete";
    }

    public static class Portal
    {
        public const string View = "Portal.View";
        public const string SelectModule = "Portal.SelectModule";
    }

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        P(Organization.View, "View organization", "View organization details.", "Organization", "Index"),
        P(Organization.Edit, "Edit organization", "Edit organization details.", "Organization", "Index"),
        P(OfficeBranches.View, "View office branches", "View and search office branches.", "Organization", "OfficeBranches"),
        P(OfficeBranches.Create, "Create office branches", "Create office branches.", "Organization", "OfficeBranches"),
        P(OfficeBranches.Edit, "Edit office branches", "Edit office branches.", "Organization", "OfficeBranches"),
        P(OfficeBranches.Delete, "Delete office branches", "Deactivate office branches.", "Organization", "OfficeBranches"),
        P(Departments.View, "View departments", "View and search departments.", "Organization", "Departments"),
        P(Departments.Create, "Create departments", "Create departments.", "Organization", "Departments"),
        P(Departments.Edit, "Edit departments", "Edit departments.", "Organization", "Departments"),
        P(Departments.Delete, "Delete departments", "Deactivate departments.", "Organization", "Departments"),
        P(FiscalYears.View, "View fiscal years", "View and search fiscal years.", "Organization", "FiscalYears"),
        P(FiscalYears.Create, "Create fiscal years", "Create draft or active fiscal years.", "Organization", "FiscalYears"),
        P(FiscalYears.Edit, "Edit fiscal years", "Edit non-closed fiscal years.", "Organization", "FiscalYears"),
        P(FiscalYears.Close, "Close fiscal years", "Permanently close fiscal years.", "Organization", "FiscalYears"),
        P(FiscalYears.Delete, "Delete fiscal years", "Delete draft fiscal years.", "Organization", "FiscalYears"),
        P(ListItemCategories.View, "View list item categories", "View list item categories.", "ListItem", "Index"),
        P(ListItemCategories.Create, "Create list item categories", "Create list item categories.", "ListItem", "Index"),
        P(ListItemCategories.Edit, "Edit list item categories", "Edit list item categories.", "ListItem", "Index"),
        P(ListItemCategories.Delete, "Delete list item categories", "Deactivate list item categories.", "ListItem", "Index"),
        P(ListItems.View, "View list items", "View list items.", "ListItem", "ListItem"),
        P(ListItems.Create, "Create list items", "Create list items.", "ListItem", "ListItem"),
        P(ListItems.Edit, "Edit list items", "Edit list items.", "ListItem", "ListItem"),
        P(ListItems.Delete, "Delete list items", "Deactivate list items.", "ListItem", "ListItem"),
        new(PermitApplicationGuidelines.View, "View permit application guidelines", "View and search permit-type guidelines.", OrganizationModule, "Setup", "PermitApplicationGuidelines", false),
        new(PermitApplicationGuidelines.Create, "Create permit application guidelines", "Create permit-type guidelines.", OrganizationModule, "Setup", "PermitApplicationGuidelines", false),
        new(PermitApplicationGuidelines.Edit, "Edit permit application guidelines", "Edit permit-type guidelines.", OrganizationModule, "Setup", "PermitApplicationGuidelines", false),
        new(PermitApplicationGuidelines.Delete, "Deactivate permit application guidelines", "Deactivate permit-type guidelines.", OrganizationModule, "Setup", "PermitApplicationGuidelines", false),
        P(Roles.View, "View roles", "View roles and module options.", "Setup", "Role"),
        P(Roles.Create, "Create roles", "Create roles and initial module assignments.", "Setup", "Role"),
        P(Roles.Edit, "Edit roles", "Edit roles and module assignments.", "Setup", "Role"),
        P(Roles.Delete, "Delete roles", "Deactivate roles.", "Setup", "Role"),
        P(Roles.AssignUsers, "Assign users to roles", "Assign or remove individual role memberships.", "Setup", "Role"),
        P(Modules.View, "View modules", "View modules, menus, and configuration.", "Setup", "Module"),
        P(Modules.Create, "Create modules", "Create application modules and menus.", "Setup", "Module"),
        P(Modules.Edit, "Edit modules", "Edit application modules and menus.", "Setup", "Module"),
        P(Modules.Delete, "Delete modules", "Deactivate application modules.", "Setup", "Module"),
        P(Modules.Configure, "Configure modules", "Configure module roles and menu assignments.", "Setup", "Module"),
        P(RoleModuleMenus.View, "View role menu assignments", "View role module-menu assignments.", "RoleModuleMenu", "Index"),
        P(RoleModuleMenus.Create, "Create role menu assignments", "Assign menus to roles.", "RoleModuleMenu", "Index"),
        P(RoleModuleMenus.Edit, "Edit role menu assignments", "Edit role menu assignments.", "RoleModuleMenu", "Index"),
        P(RoleModuleMenus.Delete, "Delete role menu assignments", "Deactivate role menu assignments.", "RoleModuleMenu", "Index"),
        P(Users.View, "View users", "View users and organization options.", "User", "Index"),
        P(Users.Create, "Create users", "Create users.", "User", "Index"),
        P(Users.Edit, "Edit users", "Edit users.", "User", "Index"),
        P(Users.Delete, "Delete users", "Deactivate users.", "User", "Index"),
        P(Users.AssignRoles, "Assign user roles", "Assign roles to users.", "User", "Index"),
        P(Workflows.View, "View workflows", "View workflow configuration.", "Workflow", "Index"),
        P(Workflows.Create, "Create workflows", "Create workflow configuration.", "Workflow", "Index"),
        P(Workflows.Edit, "Edit workflows", "Edit workflow configuration.", "Workflow", "Index"),
        P(Workflows.Delete, "Delete workflows", "Deactivate workflow configuration.", "Workflow", "Index"),
        P(AuditLogs.View, "View audit logs", "View and filter audit logs.", "AuditLog", "Index"),
        P(AuditLogs.Export, "Export audit logs", "Export filtered audit logs.", "AuditLog", "Index"),
        P(PermissionAssignments.View, "View permission assignments", "View role permission assignments.", "Setup", "Permission"),
        P(PermissionAssignments.Create, "Create permission assignments", "Grant permissions to roles.", "Setup", "Permission"),
        P(PermissionAssignments.Edit, "Edit permission assignments", "Activate or deactivate role permissions.", "Setup", "Permission"),
        P(PermissionAssignments.Manage, "Bulk manage permission assignments", "Replace a role's active permission assignments in one operation.", "Setup", "Permission"),
        P(PermissionAssignments.Delete, "Delete permission assignments", "Revoke permissions from roles.", "Setup", "Permission"),
        PermitP(PermitDashboard.View, "View permit dashboard", "Open the permit dashboard.", "PermitDashboard"),
        PermitP(PermitDashboard.Export, "Export permit dashboard", "Export permit dashboard reports.", "PermitDashboard"),
        PermitP(PermitApplications.View, "View permit applications", "View and search permit applications.", "PermitApplications"),
        PermitP(PermitApplications.Create, "Create permit applications", "Create permit applications.", "PermitApplications"),
        PermitP(PermitApplications.Edit, "Edit permit applications", "Edit permit applications while their workflow state permits changes.", "PermitApplications"),
        PermitP(PermitApplications.Finalize, "Finalize permit applications", "Finalize and submit permit applications.", "PermitApplications"),
        PermitP(PermitApplications.Complete, "Complete permit applications", "Mark permit work as completed.", "PermitApplications"),
        PermitP(PermitApplications.Cancel, "Cancel permit applications", "Cancel or close permit applications.", "PermitApplications"),
        PermitP(PermitApplications.Print, "Print permit applications", "Print permit application records.", "PermitApplications"),
        PermitP(RiskAssessments.View, "View risk assessments", "View risk assessments and their related permit applications.", "PermitApplications"),
        PermitP(RiskAssessments.Create, "Create risk assessments", "Create risk assessments.", "PermitApplications"),
        PermitP(RiskAssessments.Edit, "Edit risk assessments", "Edit draft risk assessments.", "PermitApplications"),
        PermitP(RiskAssessments.Submit, "Submit risk assessments", "Submit risk assessments into the approval workflow.", "PermitApplications"),
        PermitP(PermitApprovals.View, "View permit approvals", "View pending and historical permit approvals.", "PermitApprovals"),
        PermitP(PermitApprovals.Decide, "Decide permit approvals", "Approve or reject assigned permit approvals.", "PermitApprovals"),
        PermitP(PermitApprovals.ViewAssignments, "View approval assignments", "View pending approval assignments across approvers.", "PermitApprovals"),
        PermitP(PermitApprovals.ManageAlternateApprovers, "Manage alternate approvers", "Assign alternate users for permit approvals.", "PermitApprovals"),
        new(Portal.View, "Open portal", "Open the application portal.", PortalModule, "Portal", "Index", false),
        new(Portal.SelectModule, "Select portal modules", "Open a module from the portal.", PortalModule, "Portal", "Module", false)
    ];

    private static PermissionDefinition P(
        string code, string name, string description, string controller, string action) =>
        new(code, name, description, OrganizationModule, controller, action);

    private static PermissionDefinition PermitP(
        string code, string name, string description, string controller) =>
        new(code, name, description, PermitModule, controller, "Index");
}
