namespace CleanCrud.Application.DTOs;

public sealed record ListItemDto(
    int Id,
    int ListItemCategoryId,
    string Code,
    string Name,
    string? Description,
    int DisplayOrder);
