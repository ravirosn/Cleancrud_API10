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
            return await _context.Users.FirstOrDefaultAsync(x => x.UserName == userName);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
         
        }
        
    }
}
