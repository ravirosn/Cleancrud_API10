using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Infrastructure.Repositories
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
