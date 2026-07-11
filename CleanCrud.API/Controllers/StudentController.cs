using CleanCrud.Application.Common;
using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using CleanCrud.Domain.Entities;
using CleanCrud.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers
{

    
    [Route("api/[controller]")]
    [ApiController]
   
    public class StudentController : ControllerBase
    {
        //private readonly IStudentRepository _repository;
        private readonly IStudentService _service;
        //public StudentController(IStudentRepository repository)
        //{
        //    _repository = repository;
        //}
        public StudentController(IStudentService service)
        {
            _service = service;
        }
        [HttpGet]
        //[Authorize]
        [Authorize(Roles = "Admin")]
        //public async Task<IActionResult> GetAll()
        //{
        //    //var data = await _repository.GetAllAsync();
        //    var data = await _service.GetAllAsync();

        //    //return Ok(data);
        //    return Ok(new ApiResponse<List<StudentDto>>
        //    {
        //        Success = true,
        //        Message = "Data Fetched Successfully",
        //        Data = data
        //    });
        //}
       
        public async Task<IActionResult> GetAll( int pageNumber = 1,int pageSize = 10)
        {
            var data = await _service .GetPagedAsync(pageNumber, pageSize);
            return Ok(data);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            //var data = await _repository.GetByIdAsync(id);
            var data = await _service.GetByIdAsync(id);

            //if (data == null)
            //    return NotFound();

            //return Ok(data);

            if (data == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Student Not Found"
                });
            }

            return Ok(new ApiResponse<StudentDto>
            {
                Success = true,
                Message = "Student Found",
                Data = data
            });
        }

        //[HttpPost]
        //public async Task<IActionResult> Create(Student student)
        //{
        //    await _repository.AddAsync(student);
        //    return Ok("Student Added");
        //}
        [HttpPost]
        public async Task<IActionResult> Create(StudentDto dto)
        {
            await _service.AddAsync(dto);

            //return Ok("Student Added");
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Student Added Successfully"
            });
        }


        //[HttpPut]
        //public async Task<IActionResult> Update(Student student)
        //{
        //    await _repository.UpdateAsync(student);
        //    return Ok("Student Updated");
        //}
        [HttpPut]
        public async Task<IActionResult> Update(StudentDto dto)
        {
            await _service.UpdateAsync(dto);

            //return Ok("Student Updated");

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Student Updated Successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            //await _repository.DeleteAsync(id);
            await _service.DeleteAsync(id);
            //return Ok("Student Deleted");

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Student Deleted Successfully"
            });
        }
    }
}
