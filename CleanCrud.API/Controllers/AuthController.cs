using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        //private readonly IJwtService _jwtService;

        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        //public AuthController(IJwtService jwtService)
        //{
        //    _jwtService = jwtService;
        //}

        public AuthController(IUserService userService,IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        //[HttpPost("login")]
        //public IActionResult Login(LoginDto dto)
        //{
        //    if (dto.UserName == "admin" &&
        //        dto.Password == "123")
        //    {
        //        var token = _jwtService.GenerateToken(dto.UserName);

        //        return Ok(new
        //        {
        //            Success = true,
        //            Token = token
        //        });
        //    }

        //    return Unauthorized(new
        //    {
        //        Success = false,
        //        Message = "Invalid Username or Password"
        //    });
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            //var hash = BCrypt.Net.BCrypt.HashPassword("123");
            var user = await _userService.LoginAsync(dto);
          
            if (user == null)
                return Unauthorized("Invalid Credentials");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                UserName = user.UserName,
                Role = user.Role,
                Token = token
            });
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            await _userService.AddUserAsync(dto);
            return Ok(new
            {
                Success = true,
                Message = "User Registered Successfully"
            });
        }

        [HttpGet("test-log")]  // Only For Testing Data 
        public IActionResult TestLog()
        {
            throw new Exception("Testing Serilog");
        }

        // Pankaj Kumar Add New Branch
        // I am Changes IN Master Branceh So naw you can pull the changes in your branch and work on it
        // I am Changes IN Pankaj Branceh Branceh So naw you can pull the changes in your branch and work on it
        // I am Changes IN Pankaj Branceh Branceh So naw you can pull the changes in your branch and work on it
    }
}
