namespace CleanCrud.Application.DTOs;

public sealed record EntraUserProfileDto(Guid TenantId, Guid ObjectId,
    string UserName, string? DisplayName, string? Email);

public sealed record AuthenticatedUserDto(int UserId, IReadOnlyList<string> Roles);
