using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Infrastructure.Services
{
    public class UserService : IUserService
    {
        //private readonly IUserRepository _userRepository;

        //public UserService(IUserRepository userRepository)
        //{
        //    _userRepository = userRepository;
        //}
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;

        public UserService(
            IUserRepository userRepository,
            IPasswordService passwordService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
        }
        public async Task<User?> LoginAsync(LoginDto dto)
        {
            if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
                return null;

            var user = await _userRepository.GetByUserNameAsync(dto.UserName);

            if (user == null || !user.IsActive || user.PasswordHash is null)
                return null;

            //if (user.Password != dto.Password)
            //    return null;
            if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task AddUserAsync(RegisterDto dto)
        {
            if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
                throw new ArgumentException("Password must not exceed 72 UTF-8 bytes.");

            if (await _userRepository.GetByUserNameAsync(dto.UserName) is not null)
                throw new ArgumentException("Username is already in use.");

            var hashedPassword = _passwordService.HashPassword(dto.Password);
            var user = new User
            {
                UserName = dto.UserName.Trim(),
                NormalizedUserName = dto.UserName.Trim().ToUpperInvariant(),
                PasswordHash = hashedPassword,
                IsActive = true
            };
            await _userRepository.AddUserAsync(user);
        }


    }
}
