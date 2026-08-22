using Apcloudpms.Application.DTOs;
using Apcloudpms.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Apcloudpms.Application.Interfaces
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
