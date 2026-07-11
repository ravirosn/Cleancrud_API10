using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CleanCrud.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var allowedExtensions = new[]
             {
                 ".pdf",
                 ".jpg",
                 ".jpeg",
                 ".png"
             };

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception(
                    "Only PDF, JPG, JPEG and PNG files are allowed");
            }
            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                throw new Exception( "File size cannot exceed 5 MB");
            }
            if (file == null || file.Length == 0)
                throw new Exception("Please select a file");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName =
                Guid.NewGuid() +
                Path.GetExtension(file.FileName);

            var filePath =
                Path.Combine(uploadsFolder, fileName);

            using (var stream =
                new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }
    }
}