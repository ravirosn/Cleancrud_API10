using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Domain.Entities
{
   
    public class Student
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
    }
}
