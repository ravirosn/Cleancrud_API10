using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.DTOs
{
    public class LoginDto
    {
        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
