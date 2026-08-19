using CleanCrud.Application.DTOs;
using CleanCrud.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCrud.API.Controllers;

[ApiController]
[Route("api/list-items")]
[Authorize]
public sealed class ListItemsController : ControllerBase
{
    private readonly IListItemService _service;

    public ListItemsController(IListItemService service) => _service = service;

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
