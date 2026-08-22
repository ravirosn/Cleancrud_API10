using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IListItemService
{
    Task<IReadOnlyList<ListItemDto>> GetByCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken);
}
