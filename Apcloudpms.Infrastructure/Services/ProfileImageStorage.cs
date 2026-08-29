using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Apcloudpms.Infrastructure.Services;

public sealed class ProfileImageStorage(
    IHostEnvironment environment,
    IOptions<ProfileImageStorageOptions> options) : IProfileImageStorage
{
    private readonly ProfileImageStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid():N}{extension}";
        content.Position = 0;

        if (_options.UseAzure)
        {
            var container = GetAzureContainer();
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            }, cancellationToken);
            return $"azure:{fileName}";
        }

        var folder = GetLocalFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        await using var destination = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await content.CopyToAsync(destination, cancellationToken);
        return $"local:{fileName}";
    }

    public async Task<StoredProfileImage?> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (storagePath.StartsWith("azure:", StringComparison.Ordinal))
        {
            var fileName = GetSafeFileName(storagePath[6..]);
            var response = await GetAzureContainer().GetBlobClient(fileName)
                .DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new StoredProfileImage(
                response.Value.Content,
                response.Value.Details.ContentType ?? "image/jpeg");
        }

        if (!storagePath.StartsWith("local:", StringComparison.Ordinal))
        {
            return null;
        }

        var localFileName = GetSafeFileName(storagePath[6..]);
        var fullPath = Path.Combine(GetLocalFolder(), localFileName);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return new StoredProfileImage(stream, "image/jpeg");
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (storagePath.StartsWith("azure:", StringComparison.Ordinal))
        {
            var fileName = GetSafeFileName(storagePath[6..]);
            await GetAzureContainer().GetBlobClient(fileName)
                .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            return;
        }

        if (storagePath.StartsWith("local:", StringComparison.Ordinal))
        {
            var fileName = GetSafeFileName(storagePath[6..]);
            var fullPath = Path.Combine(GetLocalFolder(), fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    private BlobContainerClient GetAzureContainer()
    {
        if (string.IsNullOrWhiteSpace(_options.Azure.ConnectionString) ||
            string.IsNullOrWhiteSpace(_options.Azure.ContainerName))
        {
            throw new InvalidOperationException("Azure profile image storage is enabled but is not configured.");
        }

        return new BlobContainerClient(
            _options.Azure.ConnectionString,
            _options.Azure.ContainerName);
    }

    private string GetLocalFolder()
    {
        var configured = _options.LocalFolder.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }

    private static string GetSafeFileName(string value)
    {
        var fileName = Path.GetFileName(value);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != value)
        {
            throw new InvalidDataException("The profile image storage path is invalid.");
        }

        return fileName;
    }
}
