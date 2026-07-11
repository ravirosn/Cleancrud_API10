using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
namespace CleanCrud.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            // return await _context.Students.ToListAsync();

            return await _context.Students.FromSqlRaw("EXEC sp_GetStudents").ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            //return await _context.Students.FindAsync(id);


            // return await _context.Students.FromSqlInterpolated($"EXEC sp_GetStudentById {id}").FirstOrDefaultAsync();

            var data = await _context.Students
            .FromSqlInterpolated($"EXEC sp_GetStudentById {id}")
            .ToListAsync();

            return data.FirstOrDefault();
        }

        public async Task AddAsync(Student student)
        {
            //await _context.Students.AddAsync(student);
            //await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC sp_AddStudent
            @Name={student.Name},
            @Email={student.Email},
            @MobileNo={student.MobileNo}");

        }

        public async Task UpdateAsync(Student student)
        {
            //_context.Students.Update(student);
            //await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
        EXEC sp_UpdateStudent
            @Id={student.Id},
            @Name={student.Name},
            @Email={student.Email},
            @MobileNo={student.MobileNo}");
        }

        public async Task DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student != null)
            {
                //_context.Students.Remove(student);
                //await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
        EXEC sp_DeleteStudent
            @Id={id}");
            }
        }
        public async Task<PagedResponse<Student>> GetPagedAsync( int pageNumber, int pageSize)
        {
            var totalRecords = await _context.Students.CountAsync();
            var data = await _context.Students
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<Student>
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = data
            };
        }
    }
}
