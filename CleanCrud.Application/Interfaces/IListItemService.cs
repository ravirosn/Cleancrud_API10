using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IListItemService
{
    Task<IReadOnlyList<ListItemDto>> GetByCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken);
}
