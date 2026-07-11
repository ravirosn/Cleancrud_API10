using Microsoft.AspNetCore.Http;

namespace CleanCrud.Application.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}