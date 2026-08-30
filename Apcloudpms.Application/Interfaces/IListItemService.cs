using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IListItemService
{
    Task<IReadOnlyList<ListItemDto>> GetByCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken);
    Task<ListItemManagementPagedResponseDto<ListItemCategoryGridDto>> GetCategoriesAsync(
        ListItemManagementQueryDto query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListItemCategoryOptionDto>> GetCategoryOptionsAsync(CancellationToken cancellationToken);
    Task<ListItemCategoryManagementDto> CreateCategoryAsync(
        ListItemCategoryRequestDto request, CancellationToken cancellationToken);
    Task<ListItemCategoryManagementDto?> UpdateCategoryAsync(
        int id, ListItemCategoryRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken);
    Task<ListItemManagementPagedResponseDto<ListItemGridDto>> GetItemsAsync(
        ListItemQueryDto query, CancellationToken cancellationToken);
    Task<ListItemManagementDto> CreateItemAsync(
        ListItemRequestDto request, CancellationToken cancellationToken);
    Task<ListItemManagementDto?> UpdateItemAsync(
        int id, ListItemRequestDto request, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(int id, CancellationToken cancellationToken);
}
