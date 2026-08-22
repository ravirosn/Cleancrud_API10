using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            var normalizedUserName = userName.Trim().ToUpperInvariant();
            return await _context.Users.FirstOrDefaultAsync(x => x.NormalizedUserName == normalizedUserName);
        }

        public async Task<User?> GetByIdWithDetailsAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AsNoTracking()
                .Include(x => x.UserRoles.Where(userRole => userRole.IsActive))
                    .ThenInclude(userRole => userRole.Role)
                .Include(x => x.Department)
                    .ThenInclude(department => department!.OfficeBranch)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        }

        public async Task AddUserAsync(User user)
        {
            var defaultRole = await _context.Roles.SingleOrDefaultAsync(
                x => x.NormalizedName == "USER" && x.IsActive);
            if (defaultRole is null)
                throw new InvalidOperationException("The active default User role is not configured.");

            user.UserRoles.Add(new UserRole
            {
                RoleId = defaultRole.Id,
                IsActive = true,
                AssignedAtUtc = DateTime.UtcNow
            });
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
         
        }
        
    }
}
