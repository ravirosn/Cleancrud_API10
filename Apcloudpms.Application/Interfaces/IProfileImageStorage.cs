namespace Apcloudpms.Application.Interfaces;

public interface IProfileImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    Task<StoredProfileImage?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
