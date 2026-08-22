using Apcloudpms.Application.DTOs;
using Apcloudpms.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> LoginAsync(LoginDto dto);
        Task<CurrentUserDetailsDto?> GetCurrentUserDetailsAsync(
            int userId, CancellationToken cancellationToken = default);
        Task AddUserAsync(RegisterDto user);
    }
}
