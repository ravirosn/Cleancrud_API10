using CleanCrud.Domain.Entities;
using CleanCrud.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IAuditContext _auditContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, IAuditContext auditContext) : base(options)
    {
        _auditContext = auditContext;
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OfficeBranch> OfficeBranches => Set<OfficeBranch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ApplicationModule> ApplicationModules => Set<ApplicationModule>();
    public DbSet<ModuleMenu> ModuleMenus => Set<ModuleMenu>();
    public DbSet<UserModule> UserModules => Set<UserModule>();
    public DbSet<ListItemCategory> ListItemCategories => Set<ListItemCategory>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<PermitApplication> PermitApplications => Set<PermitApplication>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<RiskAssessmentHazardCategory> RiskAssessmentHazardCategories =>
        Set<RiskAssessmentHazardCategory>();
    public DbSet<RiskAssessmentSpecialPermit> RiskAssessmentSpecialPermits =>
        Set<RiskAssessmentSpecialPermit>();
    public DbSet<RiskAssessmentPpe> RiskAssessmentPpeItems => Set<RiskAssessmentPpe>();
    public DbSet<RiskAssessmentAdditionalPpe> RiskAssessmentAdditionalPpeItems =>
        Set<RiskAssessmentAdditionalPpe>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddAuditLogs();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddAuditLogs()
    {
        var auditLogs = AuditLogFactory.Create(ChangeTracker, _auditContext);
        if (auditLogs.Count > 0)
            AuditLogs.AddRange(auditLogs);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedUserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255);
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
            entity.HasIndex(x => new { x.EntraTenantId, x.EntraObjectId }).IsUnique()
                .HasFilter("[EntraTenantId] IS NOT NULL AND [EntraObjectId] IS NOT NULL");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role", "dbo");
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => x.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole", "dbo");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.AssignedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.RoleId, x.IsActive });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.TokenHash).HasColumnType("char(64)").IsRequired();
            entity.Property(x => x.ReplacedByTokenHash).HasColumnType("char(64)");
            entity.Property(x => x.CreatedByIp).HasMaxLength(45);
            entity.Property(x => x.RevokedByIp).HasMaxLength(45);
            entity.Property(x => x.RevocationReason).HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.Property(x => x.ExpiresAtUtc).HasPrecision(0);
            entity.Property(x => x.SessionExpiresAtUtc).HasPrecision(0);
            entity.Property(x => x.RevokedAtUtc).HasPrecision(0);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.FamilyId, x.RevokedAtUtc });
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OfficeBranch>(entity =>
        {
            entity.ToTable("OfficeBranch", "dbo");
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.IsHeadOffice).IsUnique()
                .HasFilter("[IsHeadOffice] = 1 AND [IsActive] = 1");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department", "dbo");
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.OfficeBranchId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.OfficeBranchId, x.IsActive });
            entity.HasOne(x => x.OfficeBranch).WithMany(x => x.Departments)
                .HasForeignKey(x => x.OfficeBranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationModule>(entity =>
        {
            entity.ToTable("ApplicationModule", "dbo");
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Icon).HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.DisplayOrder });
        });

        modelBuilder.Entity<ModuleMenu>(entity =>
        {
            entity.ToTable("ModuleMenu", "dbo");
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ControllerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ActionName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.QueryUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(100);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.ApplicationModuleId, x.QueryUrl }).IsUnique();
            entity.HasIndex(x => new { x.ApplicationModuleId, x.IsActive, x.DisplayOrder });
            entity.HasOne(x => x.ApplicationModule).WithMany(x => x.Menus)
                .HasForeignKey(x => x.ApplicationModuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ParentMenu).WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentMenuId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserModule>(entity =>
        {
            entity.ToTable("UserModule", "dbo");
            entity.HasKey(x => new { x.UserId, x.ApplicationModuleId });
            entity.Property(x => x.AssignedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.ApplicationModuleId, x.IsActive });
            entity.HasOne(x => x.User).WithMany(x => x.UserModules)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ApplicationModule).WithMany(x => x.UserModules)
                .HasForeignKey(x => x.ApplicationModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ListItemCategory>(entity =>
        {
            entity.ToTable("ListItemCategory", "dbo");
            entity.Property(x => x.Id).HasColumnName("ListItemCategoryId");
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasColumnName("CategoryName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<ListItem>(entity =>
        {
            entity.ToTable("ListItem", "dbo");
            entity.Property(x => x.Id).HasColumnName("ListItemId");
            entity.Property(x => x.Code).HasColumnName("SystemName").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasColumnName("ItemName").HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("IsVisible");
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.ListItemCategoryId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.ListItemCategoryId, x.IsActive, x.DisplayOrder });
            entity.HasOne(x => x.ListItemCategory).WithMany(x => x.ListItems)
                .HasForeignKey(x => x.ListItemCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PermitApplication>(entity =>
        {
            entity.ToTable("PermitApplication", "dbo");
            entity.Property(x => x.PermitNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IssueDate).HasColumnType("date").IsRequired();
            entity.Property(x => x.PermitIssuerName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PermitIssuerContactNumber).HasMaxLength(30);
            entity.Property(x => x.PermitReceiverName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PermitReceiverContactNumber).HasMaxLength(30);
            entity.Property(x => x.PreRiskAssessmentNumber).HasMaxLength(50);
            entity.Property(x => x.WorkLocation).HasMaxLength(500).IsRequired();
            entity.Property(x => x.WorkDescription).IsRequired();
            entity.Property(x => x.SpecialInstructions);
            entity.Property(x => x.WorkHeightBelowSurface).HasMaxLength(200);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(0);
            entity.Property(x => x.SubmittedAtUtc).HasPrecision(0);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.PermitNumber).IsUnique();
            entity.HasIndex(x => new { x.PermitStatusListItemId, x.CreatedAtUtc });
            entity.HasOne(x => x.PermitTypeListItem).WithMany(x => x.PermitTypeApplications)
                .HasForeignKey(x => x.PermitTypeListItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PermitStatusListItem).WithMany(x => x.PermitStatusApplications)
                .HasForeignKey(x => x.PermitStatusListItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.ToTable("RiskAssessment", "dbo");
            entity.Property(x => x.PreRiskAssessmentNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IssueDate).HasColumnType("date").IsRequired();
            entity.Property(x => x.PermitIssuerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PermitIssuerContact).HasMaxLength(30);
            entity.Property(x => x.PermitReceiverName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PermitReceiverContact).HasMaxLength(30);
            entity.Property(x => x.AreaResponsibleName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AreaResponsibleContact).HasMaxLength(30);
            entity.Property(x => x.LocationOfWork).HasMaxLength(255).IsRequired();
            entity.Property(x => x.DescriptionOfWork);
            entity.Property(x => x.SpecialInstructions);
            entity.Property(x => x.PlannedStartDateTime).HasPrecision(0);
            entity.Property(x => x.PlannedEndDateTime).HasPrecision(0);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RiskAssessmentHazardCategory>(entity =>
        {
            entity.ToTable("RiskAssessmentHazardCategories", "dbo");
            entity.HasKey(x => new { x.RiskAssessmentId, x.HazardCategoriesListItemId });
            entity.Property(x => x.IsSelected).HasColumnType("bit");
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.HazardCategories)
                .HasForeignKey(x => x.RiskAssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.HazardCategoryListItem)
                .WithMany(x => x.RiskAssessmentHazardCategories)
                .HasForeignKey(x => x.HazardCategoriesListItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskAssessmentSpecialPermit>(entity =>
        {
            entity.ToTable("RiskAssessmentSpecialPermit", "dbo");
            entity.HasKey(x => new { x.RiskAssessmentId, x.SpecialPermitListItemId });
            entity.Property(x => x.IsSelected).HasColumnType("bit");
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.SpecialPermits)
                .HasForeignKey(x => x.RiskAssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SpecialPermitListItem)
                .WithMany(x => x.RiskAssessmentSpecialPermits)
                .HasForeignKey(x => x.SpecialPermitListItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskAssessmentPpe>(entity =>
        {
            entity.ToTable("RiskAssessmentPPE", "dbo");
            entity.HasKey(x => new { x.RiskAssessmentId, x.SpecialPermitListItemId });
            entity.Property(x => x.IsSelected).HasColumnType("bit");
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.PersonalProtectiveEquipment)
                .HasForeignKey(x => x.RiskAssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SpecialPermitListItem)
                .WithMany(x => x.RiskAssessmentPpeItems)
                .HasForeignKey(x => x.SpecialPermitListItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskAssessmentAdditionalPpe>(entity =>
        {
            entity.ToTable("RiskAssessmentAdditionalPPE", "dbo");
            entity.HasKey(x => new { x.RiskAssessmentId, x.AdditionalProtectiveMeasuresListItemId });
            entity.Property(x => x.IsSelected).HasColumnType("bit");
            entity.HasOne(x => x.RiskAssessment)
                .WithMany(x => x.AdditionalPersonalProtectiveEquipment)
                .HasForeignKey(x => x.RiskAssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AdditionalProtectiveMeasureListItem)
                .WithMany(x => x.RiskAssessmentAdditionalPpeItems)
                .HasForeignKey(x => x.AdditionalProtectiveMeasuresListItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog", "dbo");
            entity.Property(x => x.EntityName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ChangedBy).HasMaxLength(256);
            entity.Property(x => x.TraceId).HasMaxLength(100);
            entity.Property(x => x.IpAddress).HasMaxLength(45);
            entity.Property(x => x.ChangedAtUtc).HasPrecision(0);
            entity.HasIndex(x => new { x.EntityName, x.ChangedAtUtc });
            entity.HasIndex(x => new { x.ChangedByUserId, x.ChangedAtUtc });
        });

        SeedModulesAndMenus(modelBuilder);
        SeedListItems(modelBuilder);
    }

    private static void SeedModulesAndMenus(ModelBuilder modelBuilder)
    {
        var seededAt = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ApplicationModule>().HasData(
            new ApplicationModule { Id = 1, Code = "PERMIT", Name = "Permit Management System", Description = "Manage permit applications, reviews, and approvals.", Icon = "file-check", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt },
            new ApplicationModule { Id = 2, Code = "VISITOR", Name = "Visitor Management System", Description = "Manage visitor registration, check-in, and visit history.", Icon = "users", DisplayOrder = 2, IsActive = true, CreatedAtUtc = seededAt },
            new ApplicationModule { Id = 3, Code = "ASSET", Name = "Asset Management System", Description = "Manage organizational assets and assignments.", Icon = "package", DisplayOrder = 3, IsActive = true, CreatedAtUtc = seededAt },
            new ApplicationModule { Id = 4, Code = "POWERBI", Name = "Analytics and Reports", Description = "View embedded Power BI dashboards and reports.", Icon = "bar-chart", DisplayOrder = 4, IsActive = true, CreatedAtUtc = seededAt });

        modelBuilder.Entity<ModuleMenu>().HasData(
            new ModuleMenu { Id = 1, ApplicationModuleId = 1, Name = "Dashboard", ControllerName = "PermitDashboard", ActionName = "Index", QueryUrl = "/api/permit/dashboard", Icon = "dashboard", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 2, ApplicationModuleId = 1, Name = "Permit Applications", ControllerName = "PermitApplications", ActionName = "Index", QueryUrl = "/api/permit/applications", Icon = "file-text", DisplayOrder = 2, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 3, ApplicationModuleId = 1, Name = "Permit Approvals", ControllerName = "PermitApprovals", ActionName = "Index", QueryUrl = "/api/permit/approvals", Icon = "check-circle", DisplayOrder = 3, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 4, ApplicationModuleId = 2, Name = "Dashboard", ControllerName = "VisitorDashboard", ActionName = "Index", QueryUrl = "/api/visitor/dashboard", Icon = "dashboard", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 5, ApplicationModuleId = 2, Name = "Visitor Check-In", ControllerName = "VisitorCheckIn", ActionName = "Index", QueryUrl = "/api/visitor/check-in", Icon = "log-in", DisplayOrder = 2, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 6, ApplicationModuleId = 2, Name = "Visitor Log", ControllerName = "VisitorLog", ActionName = "Index", QueryUrl = "/api/visitor/log", Icon = "list", DisplayOrder = 3, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 7, ApplicationModuleId = 3, Name = "Dashboard", ControllerName = "AssetDashboard", ActionName = "Index", QueryUrl = "/api/asset/dashboard", Icon = "dashboard", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 8, ApplicationModuleId = 3, Name = "Asset Register", ControllerName = "AssetRegister", ActionName = "Index", QueryUrl = "/api/asset/register", Icon = "archive", DisplayOrder = 2, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 9, ApplicationModuleId = 3, Name = "Asset Assignments", ControllerName = "AssetAssignments", ActionName = "Index", QueryUrl = "/api/asset/assignments", Icon = "user-check", DisplayOrder = 3, IsActive = true, CreatedAtUtc = seededAt },
            new ModuleMenu { Id = 10, ApplicationModuleId = 4, Name = "Power BI Report", ControllerName = "PowerBi", ActionName = "GetEmbedConfig", QueryUrl = "/api/power-bi/embed-config", Icon = "bar-chart-2", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt });
    }

    private static void SeedListItems(ModelBuilder modelBuilder)
    {
        var seededAt = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ListItemCategory>().HasData(
            new ListItemCategory { Id = 1, Code = "PERMIT_STATUS", Name = "Permit Status", Description = "Workflow statuses for permit applications.", IsActive = true, CreatedAtUtc = seededAt },
            new ListItemCategory { Id = 2, Code = "PERMIT_TYPE", Name = "Permit Type", Description = "Available permit application types.", IsActive = true, CreatedAtUtc = seededAt });

        modelBuilder.Entity<ListItem>().HasData(
            new ListItem { Id = 1, ListItemCategoryId = 1, Code = "DRAFT", Name = "Draft", DisplayOrder = 1, IsActive = true, CreatedAtUtc = seededAt },
            new ListItem { Id = 2, ListItemCategoryId = 1, Code = "SUBMITTED_FOR_APPROVAL", Name = "Submitted For Approval", DisplayOrder = 2, IsActive = true, CreatedAtUtc = seededAt },
            new ListItem { Id = 3, ListItemCategoryId = 1, Code = "APPROVED", Name = "Approved", DisplayOrder = 3, IsActive = true, CreatedAtUtc = seededAt },
            new ListItem { Id = 4, ListItemCategoryId = 1, Code = "REJECTED", Name = "Rejected", DisplayOrder = 4, IsActive = true, CreatedAtUtc = seededAt },
            new ListItem { Id = 5, ListItemCategoryId = 1, Code = "DELETED", Name = "Deleted", DisplayOrder = 5, IsActive = true, CreatedAtUtc = seededAt });
    }
}
