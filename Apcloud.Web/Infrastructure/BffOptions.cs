namespace Apcloud.Web.Infrastructure;

public sealed class BffOptions
{
    public const string SectionName = "Bff";

    public string[] AllowedPathPrefixes { get; init; } = [];

    public string[] AllowedMethods { get; init; } = ["GET"];
}
