using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string NormalizedUserName { get; set; } = string.Empty;

        public string? PasswordHash { get; set; }

        public Guid? EntraTenantId { get; set; }

        public Guid? EntraObjectId { get; set; }

        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public string? ContactNumber { get; set; }

        public string? ProfilePicturePath { get; set; }

        public DateTime? ProfilePictureUpdatedAtUtc { get; set; }

        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        public DateTime? ModifiedAtUtc { get; set; }

        public int? ModifiedByUserId { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public UserThemeSetting? ThemeSetting { get; set; }

        public Department? Department { get; set; }
    }
}
