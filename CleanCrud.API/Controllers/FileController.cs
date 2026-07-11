using CleanCrud.Application.Common;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var fileName =
                await _fileService.UploadFileAsync(file);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "File Uploaded Successfully",
                Data = fileName
            });
        }
    }
}