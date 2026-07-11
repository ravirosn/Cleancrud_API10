using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string MobileNo { get; set; } = string.Empty;
    }
}
