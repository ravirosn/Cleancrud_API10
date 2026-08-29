namespace Apcloud.Web.Areas.PermitApplication.Models;

public sealed class RiskAssessmentListViewModel
{
    public IReadOnlyList<RiskAssessmentListItemViewModel> Items { get; init; } = [];

    public IReadOnlyList<RiskAssessmentColumnViewModel> Columns { get; init; } = [];

    public string SearchTerm { get; init; } = string.Empty;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int FirstItemNumber => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemNumber => Math.Min(PageNumber * PageSize, TotalCount);

    public string? ErrorMessage { get; init; }
}

public sealed class RiskAssessmentListItemViewModel
{
    public required string Id { get; init; }

    public required string Reference { get; init; }

    public required string Title { get; init; }

    public string? PermitNumber { get; init; }

    public string? Category { get; init; }

    public string? RiskLevel { get; init; }

    public string? Status { get; init; }

    public string? AssessedBy { get; init; }

    public DateTimeOffset? AssessmentDate { get; init; }

    public IReadOnlyDictionary<string, string> Fields { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record RiskAssessmentColumnViewModel(string Key, string Label);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
