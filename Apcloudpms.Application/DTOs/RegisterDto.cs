using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Apcloudpms.Application.DTOs
{
    public class RegisterDto
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 12)]
        public string Password { get; set; } = string.Empty;
    }
    
}
