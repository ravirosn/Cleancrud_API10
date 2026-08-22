using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
        //public int StatusCode { get; set; }

        public T? Data { get; set; }
    }
}
