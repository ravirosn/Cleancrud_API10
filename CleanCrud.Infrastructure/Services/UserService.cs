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
            var user = await _userRepository.GetByUserNameAsync(dto.UserName);

            if (user == null)
                return null;

            //if (user.Password != dto.Password)
            //    return null;
            if (!_passwordService.VerifyPassword(dto.Password, user.Password))
                return null;

            return user;
        }

        public async Task AddUserAsync(RegisterDto dto)
        {
            var hashedPassword = _passwordService.HashPassword(dto.Password);
            var user = new User
            {
                UserName = dto.UserName,
                Password = hashedPassword,
                Role = dto.Role
            };
            await _userRepository.AddUserAsync(user);
        }


    }
}
