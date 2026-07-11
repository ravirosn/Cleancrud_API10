using CleanCrud.Application.DTOs;
using CleanCrud.Domain.Entities;
using AutoMapper;

namespace CleanCrud.Application.Mappings
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            CreateMap<Student, StudentDto>();

            CreateMap<StudentDto, Student>();
        }
    }
}
