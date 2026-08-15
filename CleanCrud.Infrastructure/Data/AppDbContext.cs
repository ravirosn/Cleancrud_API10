using CleanCrud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanCrud.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

        SeedModulesAndMenus(modelBuilder);
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
}
