using CleanCrud.Application.DTOs;
using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IStudentRepository
    {
        Task<List<Student>> GetAllAsync();
        Task<PagedResponse<Student>> GetPagedAsync(int pageNumber,int pageSize);
        Task<Student?> GetByIdAsync(int id);

        Task AddAsync(Student student);

        Task UpdateAsync(Student student);

        Task DeleteAsync(int id);
    }
}
