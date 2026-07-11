using CleanCrud.Application.DTOs;
using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> LoginAsync(LoginDto dto);
        Task AddUserAsync(RegisterDto user);
    }
}
