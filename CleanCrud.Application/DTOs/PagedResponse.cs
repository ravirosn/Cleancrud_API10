using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.DTOs
{
    public class PagedResponse<T>
    {
        public int TotalRecords { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public IEnumerable<T>? Data { get; set; }
    }
}
