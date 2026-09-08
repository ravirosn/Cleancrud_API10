using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.API.Authorization;
using Apcloud.Contracts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apcloudpms.API.Controllers;

[ApiController]
[Route("api/list-items")]
[Authorize]
public sealed class ListItemsController : ControllerBase
{
    private readonly IListItemService _service;

    public ListItemsController(IListItemService service) => _service = service;

    [HttpGet("categories")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "Index")]
    [RequirePermission(ApplicationPermissions.ListItemCategories.View)]
    public async Task<ActionResult<ListItemManagementPagedResponseDto<ListItemCategoryGridDto>>> GetCategories(
        [FromQuery] ListItemManagementQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetCategoriesAsync(query, cancellationToken));

    [HttpGet("categories/ddl")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.View)]
    public async Task<ActionResult<IReadOnlyList<ListItemCategoryOptionDto>>> GetCategoryOptions(
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetCategoryOptionsAsync(cancellationToken));

    [HttpPost("categories")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "Index")]
    [RequirePermission(ApplicationPermissions.ListItemCategories.Create)]
    public async Task<ActionResult<ListItemCategoryManagementDto>> CreateCategory(
        ListItemCategoryRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await _service.CreateCategoryAsync(request, cancellationToken));

    [HttpPut("categories/{id:int}")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "Index")]
    [RequirePermission(ApplicationPermissions.ListItemCategories.Edit)]
    public async Task<ActionResult<ListItemCategoryManagementDto>> UpdateCategory(
        int id, ListItemCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateCategoryAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("categories/{id:int}")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "Index")]
    [RequirePermission(ApplicationPermissions.ListItemCategories.Delete)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken) =>
        await _service.DeleteCategoryAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.View)]
    public async Task<ActionResult<ListItemManagementPagedResponseDto<ListItemGridDto>>> GetItems(
        [FromQuery] ListItemQueryDto query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetItemsAsync(query, cancellationToken));

    [HttpPost]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.Create)]
    public async Task<ActionResult<ListItemManagementDto>> CreateItem(
        ListItemRequestDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await _service.CreateItemAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.Edit)]
    public async Task<ActionResult<ListItemManagementDto>> UpdateItem(
        int id, ListItemRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateItemAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [RequireMenu(ApplicationPermissions.OrganizationModule, "ListItem", "ListItem")]
    [RequirePermission(ApplicationPermissions.ListItems.Delete)]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken cancellationToken) =>
        await _service.DeleteItemAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("category/{categoryName}")]
    public async Task<ActionResult<IReadOnlyList<ListItemDto>>> GetByCategory(
        string categoryName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return BadRequest("Category name is required.");

        return Ok(await _service.GetByCategoryAsync(categoryName, cancellationToken));
    }
}
