using Apcloudpms.Application.DTOs;
using Apcloudpms.Domain.Entities;
using AutoMapper;

namespace Apcloudpms.Application.Mappings
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
