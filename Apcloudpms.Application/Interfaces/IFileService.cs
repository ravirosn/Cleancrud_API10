using Microsoft.AspNetCore.Http;

namespace Apcloudpms.Application.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}