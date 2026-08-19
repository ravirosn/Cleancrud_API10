using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUserNameAsync(string userName);
        Task<User?> GetByIdWithDetailsAsync(int userId, CancellationToken cancellationToken = default);
        Task AddUserAsync(User user);
    }
}
