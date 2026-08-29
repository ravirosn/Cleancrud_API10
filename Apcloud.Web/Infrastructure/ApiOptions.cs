using System.ComponentModel.DataAnnotations;

namespace Apcloud.Web.Infrastructure;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}
