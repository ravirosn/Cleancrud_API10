using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Apcloud.Contracts.Authentication;
using Apcloud.Contracts.Themes;
using Apcloud.Web.Areas.PermitApplication.Models;
using Apcloud.Web.Areas.Portal.Models;
using Apcloud.Web.Services.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Apcloud.Web.Services;

/// <summary>
/// Server-side client for authenticated Apcloud API requests. Its message
/// handler supplies and refreshes the bearer token automatically.
/// </summary>
public sealed class ApcloudApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache memoryCache)
{
    private static readonly TimeSpan ProfileCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NavigationCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] CollectionPropertyNames =
        ["data", "items", "result", "modules", "menus", "navigation", "navigationMenus"];

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default) =>
        httpClient.SendAsync(request, cancellationToken);

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken = default) =>
        httpClient.SendAsync(request, completionOption, cancellationToken);

    public Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default) =>
        httpClient.GetAsync(requestUri, cancellationToken);

    public async Task<CurrentUserDetailsDto> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey("profile");
        if (TryReadCache(cacheKey, out CurrentUserDetailsDto? cachedUser))
            return cachedUser!;

        using var response = await httpClient.GetAsync("api/Auth/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthApiException(response.StatusCode, "The current user profile could not be loaded.");
        }

        try
        {
            var user = await response.Content.ReadFromJsonAsync<CurrentUserDetailsDto>(cancellationToken)
                ?? throw new AuthApiException(HttpStatusCode.BadGateway, "The API returned an empty user profile.");
            WriteCache(GetCacheKey("profile", user.Id), user, ProfileCacheDuration);
            return user;
        }
        catch (JsonException exception)
        {
            throw new AuthApiException(HttpStatusCode.BadGateway, "The API returned an invalid user profile.", exception);
        }
    }

    public async Task<UserThemeSettingsDto> GetThemeSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/user-theme-settings", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthApiException(response.StatusCode, "The user theme settings could not be loaded.");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<UserThemeSettingsDto>(cancellationToken)
                ?? throw new AuthApiException(HttpStatusCode.BadGateway, "The API returned empty theme settings.");
        }
        catch (JsonException exception)
        {
            throw new AuthApiException(HttpStatusCode.BadGateway, "The API returned invalid theme settings.", exception);
        }
    }

    public async Task<IReadOnlyList<AssignedModuleViewModel>> GetMyModulesAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey("modules");
        if (TryReadCache(cacheKey, out AssignedModuleViewModel[]? cachedModules))
            return cachedModules!;

        using var response = await httpClient.GetAsync("api/module-access/my-modules", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);

        var modules = GetCollection(document.RootElement)
            .Select(ParseModule)
            .Where(module => module is not null && module.IsActive)
            .Cast<AssignedModuleViewModel>()
            .OrderBy(module => module.DisplayOrder)
            .ThenBy(module => module.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        WriteCache(cacheKey, modules, NavigationCacheDuration);
        return modules;
    }

    public async Task<IReadOnlyList<NavigationMenuViewModel>> SelectModuleMenusAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(moduleId, out var applicationModuleId) || applicationModuleId <= 0)
        {
            throw new ArgumentException("A valid application module ID is required.", nameof(moduleId));
        }

        var cacheKey = GetCacheKey($"menus:{applicationModuleId}");
        if (TryReadCache(cacheKey, out NavigationMenuViewModel[]? cachedMenus))
            return cachedMenus!;

        using var response = await httpClient.PostAsJsonAsync(
            "api/module-access/select",
            new { applicationModuleId },
            cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);

        var menus = GetCollection(document.RootElement)
            .Select(ParseMenu)
            .Where(menu => menu is not null && menu.IsActive)
            .Cast<NavigationMenuViewModel>()
            .ToList();

        var navigation = BuildMenuTree(menus).ToArray();
        WriteCache(cacheKey, navigation, NavigationCacheDuration);
        return navigation;
    }

    public void CacheCurrentUser(CurrentUserDetailsDto user) =>
        WriteCache(GetCacheKey("profile", user.Id), user, ProfileCacheDuration);

    public void InvalidateCurrentUserProfile()
    {
        var key = GetCacheKey("profile");
        if (key is not null) memoryCache.Remove(key);
    }

    private string? GetCacheKey(string segment, int? explicitUserId = null)
    {
        var userId = explicitUserId?.ToString() ??
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : $"apcloud-ui:{userId}:{segment}";
    }

    private bool TryReadCache<T>(string? key, out T? value)
    {
        value = default;
        if (key is null || !memoryCache.TryGetValue(key, out string? json) || string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            value = JsonSerializer.Deserialize<T>(json, CacheJsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            memoryCache.Remove(key);
            return false;
        }
    }

    private void WriteCache<T>(string? key, T value, TimeSpan duration)
    {
        if (key is null) return;
        memoryCache.Set(key, JsonSerializer.Serialize(value, CacheJsonOptions), duration);
    }

    public async Task<PagedResult<RiskAssessmentListItemViewModel>> GetRiskAssessmentsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = $"pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm.Trim())}";
        }

        using var response = await httpClient.GetAsync($"api/risk-assessments?{query}", cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        var root = document.RootElement;
        var items = GetCollection(root)
            .Select(ParseRiskAssessment)
            .Where(item => item is not null)
            .Cast<RiskAssessmentListItemViewModel>()
            .ToArray();

        var returnedPage = FindMetadataInt(root, pageNumber, "pageNumber", "currentPage", "page");
        var returnedPageSize = FindMetadataInt(root, pageSize, "pageSize", "limit", "perPage");
        var totalCount = FindMetadataInt(root, items.Length, "totalCount", "totalRecords", "recordCount", "total");

        return new PagedResult<RiskAssessmentListItemViewModel>(
            items,
            Math.Max(1, returnedPage),
            Math.Max(1, returnedPageSize),
            Math.Max(0, totalCount));
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthApiException(response.StatusCode, "The API could not complete the request.");
        }

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new AuthApiException(HttpStatusCode.BadGateway, "The API returned an invalid response.", exception);
        }
    }

    private static IEnumerable<JsonElement> GetCollection(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in CollectionPropertyNames)
            {
                if (TryGet(root, name, out var value))
                {
                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        return value.EnumerateArray();
                    }

                    if (value.ValueKind == JsonValueKind.Object)
                    {
                        var nested = GetCollection(value);
                        if (nested.Any())
                        {
                            return nested;
                        }
                    }
                }
            }
        }

        return [];
    }

    private static AssignedModuleViewModel? ParseModule(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var details = TryGet(item, "module", out var module) && module.ValueKind == JsonValueKind.Object
            ? module
            : item;
        var id = FirstString(item, "moduleId", "id") ?? FirstString(details, "id", "moduleId");
        var name = FirstString(details, "name", "moduleName", "displayName", "title")
                   ?? FirstString(item, "moduleName", "name", "displayName");

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var assignmentActive = FirstBool(item, true, "isActive", "active");
        var moduleActive = FirstBool(details, true, "isActive", "active");

        return new AssignedModuleViewModel
        {
            Id = id,
            Name = name,
            Code = FirstString(details, "code", "moduleCode"),
            Description = FirstString(details, "description", "moduleDescription", "summary"),
            Icon = FirstString(details, "icon", "iconName", "iconClass"),
            DisplayOrder = FirstInt(item, FirstInt(details, int.MaxValue, "displayOrder", "sortOrder", "order"), "displayOrder", "sortOrder", "order"),
            IsActive = assignmentActive && moduleActive
        };
    }

    private static NavigationMenuViewModel? ParseMenu(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = FirstString(item, "id", "menuId", "navigationMenuId");
        var name = FirstString(item, "name", "menuName", "displayName", "title", "label");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var menu = new NavigationMenuViewModel
        {
            Id = id,
            ParentId = FirstString(item, "parentId", "parentMenuId", "parentNavigationMenuId"),
            Name = name,
            Description = FirstString(item, "description", "summary"),
            Icon = FirstString(item, "icon", "iconName", "iconClass"),
            Url = GetSafeMenuUrl(item),
            DisplayOrder = FirstInt(item, int.MaxValue, "displayOrder", "sortOrder", "order"),
            IsActive = FirstBool(item, true, "isActive", "active")
        };

        if (TryGet(item, "children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray().Select(ParseMenu).Where(child => child is not null && child.IsActive))
            {
                menu.Children.Add(child!);
            }
        }

        return menu;
    }

    private static RiskAssessmentListItemViewModel? ParseRiskAssessment(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = FirstString(item, "riskAssessmentId", "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var reference = FirstString(item,
            "assessmentNumber", "riskAssessmentNumber", "referenceNumber", "reference", "code") ?? "—";
        var title = FirstString(item,
            "title", "name", "activityName", "taskName", "riskTitle") ?? "Risk assessment";

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CollectDisplayFields(item, fields);

        return new RiskAssessmentListItemViewModel
        {
            Id = id,
            Reference = reference,
            Title = title,
            PermitNumber = FirstString(item,
                "permitNumber", "permitReference", "permitApplicationNumber", "applicationNumber")
                ?? FirstNestedString(item, "permit", "number", "permitNumber", "referenceNumber")
                ?? FirstNestedString(item, "permitApplication", "number", "applicationNumber", "referenceNumber"),
            Category = FirstString(item, "category", "riskCategory", "assessmentType", "type")
                ?? FirstNestedString(item, "category", "name", "displayName"),
            RiskLevel = FirstString(item, "riskLevel", "overallRiskLevel", "riskRating", "rating", "level")
                ?? FirstNestedString(item, "riskLevel", "name", "displayName"),
            Status = FirstString(item, "status", "assessmentStatus", "workflowStatus")
                ?? FirstNestedString(item, "status", "name", "displayName"),
            AssessedBy = FirstString(item, "assessedByName", "assessorName", "createdByName")
                ?? FirstNestedString(item, "assessedBy", "displayName", "name", "userName")
                ?? FirstNestedString(item, "assessor", "displayName", "name", "userName"),
            AssessmentDate = FirstDate(item,
                "assessmentDate", "assessedAtUtc", "assessmentDateUtc", "createdAtUtc", "createdAt"),
            Fields = fields
        };
    }

    private static IReadOnlyList<NavigationMenuViewModel> BuildMenuTree(List<NavigationMenuViewModel> menus)
    {
        var byId = menus
            .GroupBy(menu => menu.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var roots = new List<NavigationMenuViewModel>();

        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.ParentId) &&
                !menu.ParentId.Equals(menu.Id, StringComparison.OrdinalIgnoreCase) &&
                byId.TryGetValue(menu.ParentId, out var parent) &&
                !IsDescendant(menu, parent, byId))
            {
                if (!parent.Children.Any(child => child.Id.Equals(menu.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    parent.Children.Add(menu);
                }
            }
            else
            {
                roots.Add(menu);
            }
        }

        SortMenus(roots);
        return roots;
    }

    private static bool IsDescendant(
        NavigationMenuViewModel menu,
        NavigationMenuViewModel possibleChild,
        IReadOnlyDictionary<string, NavigationMenuViewModel> byId)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { menu.Id };
        var current = possibleChild;
        while (!string.IsNullOrWhiteSpace(current.ParentId) && byId.TryGetValue(current.ParentId, out current!))
        {
            if (!seen.Add(current.Id))
            {
                return true;
            }
        }

        return false;
    }

    private static void SortMenus(List<NavigationMenuViewModel> menus)
    {
        menus.Sort((left, right) =>
        {
            var order = left.DisplayOrder.CompareTo(right.DisplayOrder);
            return order != 0 ? order : StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
        });

        foreach (var menu in menus)
        {
            SortMenus(menu.Children);
        }
    }

    private static string? GetSafeMenuUrl(JsonElement item)
    {
        var value = FirstString(item, "url", "navigationUrl", "controllerActionUrl", "route", "path");
        if (string.IsNullOrWhiteSpace(value))
        {
            var controller = FirstString(item, "controller", "controllerName");
            var action = FirstString(item, "action", "actionName");
            var area = FirstString(item, "area", "areaName");
            if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(action))
            {
                value = string.Join('/', new[] { area, controller, action }.Where(part => !string.IsNullOrWhiteSpace(part)));
            }
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.StartsWith("~/", StringComparison.Ordinal))
        {
            value = value[1..];
        }
        else if (!value.StartsWith('/'))
        {
            value = '/' + value;
        }

        return value.StartsWith("//", StringComparison.Ordinal) ||
               value.Contains('\\') ||
               value.Any(char.IsControl)
            ? null
            : value;
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? FirstString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGet(element, name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var result = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return null;
    }

    private static bool FirstBool(JsonElement element, bool fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGet(element, name, out var value))
            {
                continue;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            if (bool.TryParse(value.ToString(), out var result))
            {
                return result;
            }
        }

        return fallback;
    }

    private static int FirstInt(JsonElement element, int fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGet(element, name, out var value) && int.TryParse(value.ToString(), out var result))
            {
                return result;
            }
        }

        return fallback;
    }

    private static int FindMetadataInt(JsonElement root, int fallback, params string[] names)
    {
        var candidates = new List<JsonElement> { root };
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var containerName in new[] { "data", "result", "pagination", "paging", "meta", "metadata" })
            {
                if (TryGet(root, containerName, out var container) && container.ValueKind == JsonValueKind.Object)
                {
                    candidates.Add(container);
                    foreach (var nestedName in new[] { "pagination", "paging", "meta", "metadata" })
                    {
                        if (TryGet(container, nestedName, out var nested) && nested.ValueKind == JsonValueKind.Object)
                        {
                            candidates.Add(nested);
                        }
                    }
                }
            }
        }

        foreach (var candidate in candidates)
        {
            var value = FirstInt(candidate, int.MinValue, names);
            if (value != int.MinValue)
            {
                return value;
            }
        }

        return fallback;
    }

    private static string? FirstNestedString(
        JsonElement element,
        string propertyName,
        params string[] valueNames)
    {
        return TryGet(element, propertyName, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? FirstString(nested, valueNames)
            : null;
    }

    private static DateTimeOffset? FirstDate(JsonElement element, params string[] names)
    {
        var value = FirstString(element, names);
        return DateTimeOffset.TryParse(value, out var result) ? result : null;
    }

    private static void CollectDisplayFields(
        JsonElement element,
        IDictionary<string, string> fields,
        string? prefix = null,
        int depth = 0)
    {
        if (element.ValueKind != JsonValueKind.Object || depth > 2)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (IsIdentifierProperty(property.Name))
            {
                continue;
            }

            var key = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";
            var displayValue = FormatDisplayValue(property.Value);
            if (!string.IsNullOrWhiteSpace(displayValue))
            {
                fields.TryAdd(key, displayValue);
            }
            else if (property.Value.ValueKind == JsonValueKind.Object)
            {
                CollectDisplayFields(property.Value, fields, key, depth + 1);
            }
        }
    }

    private static bool IsIdentifierProperty(string name)
    {
        return name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("rowVersion", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("concurrencyStamp", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FormatDisplayValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var text = value.GetString();
                if (text?.Length >= 8 &&
                    (text.Contains('-', StringComparison.Ordinal) || text.Contains('/', StringComparison.Ordinal)) &&
                    DateTimeOffset.TryParse(text, out var date))
                {
                    return date.ToLocalTime().ToString("dd MMM yyyy");
                }

                return text;
            case JsonValueKind.Number:
                return value.ToString();
            case JsonValueKind.True:
                return "Yes";
            case JsonValueKind.False:
                return "No";
            case JsonValueKind.Array:
                var values = value.EnumerateArray()
                    .Select(FormatDisplayValue)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Take(4)
                    .ToArray();
                return values.Length > 0 ? string.Join(", ", values) : null;
            default:
                return null;
        }
    }
}
