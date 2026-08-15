namespace CleanCrud.Application.DTOs;

public sealed record PowerBiEmbedConfigDto(string ReportId, string ReportName,
    string EmbedUrl, string EmbedToken, DateTimeOffset Expiration);
