using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.DTOs
{
    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }
    
}
