using CleanCrud.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IStudentService
    {


        Task<List<StudentDto>> GetAllAsync();
        Task<PagedResponse<StudentDto>> GetPagedAsync( int pageNumber, int pageSize);
        Task<StudentDto?> GetByIdAsync(int id);

        Task AddAsync(StudentDto dto);

        Task UpdateAsync(StudentDto dto);

        Task DeleteAsync(int id);
    }
}
