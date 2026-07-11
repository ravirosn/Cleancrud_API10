using AutoMapper;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Infrastructure.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IMapper _mapper;

        //public StudentService(IStudentRepository repository)
        //{
        //    _repository = repository;
        //}


        public StudentService(IStudentRepository repository,IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudentDto>> GetAllAsync()
        {
            var students = await _repository.GetAllAsync();

            //return students.Select(x => new StudentDto
            //{
            //    Id = x.Id,
            //    Name = x.Name,
            //    Email = x.Email,
            //    MobileNo = x.MobileNo
            //}).ToList();
            return _mapper.Map<List<StudentDto>>(students);
        }

        public async Task AddAsync(StudentDto dto)
        {
            //var student = new Student
            //{
            //    Name = dto.Name,
            //    Email = dto.Email,
            //    MobileNo = dto.MobileNo
            //};


            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Name is required");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("Email is required");

            var student = _mapper.Map<Student>(dto);
            await _repository.AddAsync(student);

        }
        public async Task<StudentDto?> GetByIdAsync(int id)
        {
            var student = await _repository.GetByIdAsync(id);

            if (student == null)
                return null;

            //return new StudentDto
            //{
            //    Id = student.Id,
            //    Name = student.Name,
            //    Email = student.Email,
            //    MobileNo = student.MobileNo
            //};
            return _mapper.Map<StudentDto>(student);
        }
        public async Task UpdateAsync(StudentDto dto)
        {
            //var student = new Student
            //{
            //    Id = dto.Id,
            //    Name = dto.Name,
            //    Email = dto.Email,
            //    MobileNo = dto.MobileNo
            //};
            var student = _mapper.Map<Student>(dto);
            await _repository.UpdateAsync(student);

        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }


        public async Task<PagedResponse<StudentDto>> GetPagedAsync(
    int pageNumber,
    int pageSize)
        {
            var result = await _repository.GetPagedAsync(
                pageNumber,
                pageSize);

            return new PagedResponse<StudentDto>
            {
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                Data = _mapper.Map<List<StudentDto>>(result.Data)
            };
        }
    }
}
