using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Apcloudpms.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly JwtOptions _options;

    public AuthService(AppDbContext context, IPasswordService passwordService,
        IJwtService jwtService, IOptions<JwtOptions> options)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _options = options.Value;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
            return null;

        var normalizedUserName = dto.UserName.Trim().ToUpperInvariant();
        var user = await _context.Users.AsNoTracking()
            .Include(x => x.UserRoles.Where(userRole => userRole.IsActive))
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                x => x.NormalizedUserName == normalizedUserName && x.IsActive,
                cancellationToken);

        if (user is null || user.PasswordHash is null ||
            !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
            return null;

        var now = DateTime.UtcNow;
        var sessionExpiresAtUtc = now.AddDays(_options.RefreshTokenAbsoluteDays);
        var (rawToken, refreshToken) = CreateRefreshToken(
            user.Id, Guid.NewGuid(), now, sessionExpiresAtUtc, ipAddress);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
        return CreateResponse(user, rawToken, refreshToken.ExpiresAtUtc);
    }

    public async Task<AuthResponseDto?> RefreshAsync(string refreshToken, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var currentToken = await _context.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(user => user.UserRoles.Where(userRole => userRole.IsActive))
                    .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (currentToken is null)
            return null;

        var now = DateTime.UtcNow;
        if (currentToken.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(currentToken.FamilyId, now, ipAddress,
                "Refresh token reuse detected", cancellationToken);
            return null;
        }

        if (currentToken.ExpiresAtUtc <= now || currentToken.SessionExpiresAtUtc <= now ||
            !currentToken.User.IsActive)
            return null;

        var (rawReplacement, replacement) = CreateRefreshToken(currentToken.UserId,
            currentToken.FamilyId, now, currentToken.SessionExpiresAtUtc, ipAddress);

        currentToken.RevokedAtUtc = now;
        currentToken.RevokedByIp = NormalizeIp(ipAddress);
        currentToken.RevocationReason = "Rotated";
        currentToken.ReplacedByTokenHash = replacement.TokenHash;
        _context.RefreshTokens.Add(replacement);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent use won the rotation. Revoke its replacement as reuse.
            _context.ChangeTracker.Clear();
            await RevokeFamilyAsync(currentToken.FamilyId, DateTime.UtcNow, ipAddress,
                "Concurrent refresh token reuse detected", cancellationToken);
            return null;
        }

        return CreateResponse(currentToken.User, rawReplacement, replacement.ExpiresAtUtc);
    }

    public async Task<bool> RevokeAsync(string refreshToken, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var token = await _context.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (token is null)
            return false;

        await RevokeFamilyAsync(token.FamilyId, DateTime.UtcNow, ipAddress,
            "Revoked by client", cancellationToken);
        return true;
    }

    private AuthResponseDto CreateResponse(User user, string rawRefreshToken,
        DateTime refreshExpiresAtUtc)
    {
        var roles = user.UserRoles
            .Where(x => x.IsActive && x.Role.IsActive)
            .Select(x => x.Role.Name);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        return new AuthResponseDto(accessToken.Token, rawRefreshToken,
            accessToken.ExpiresAtUtc, refreshExpiresAtUtc);
    }

    private (string RawToken, RefreshToken Token) CreateRefreshToken(int userId, Guid familyId,
        DateTime now, DateTime sessionExpiresAtUtc, string? ipAddress)
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var expiresAtUtc = now.AddDays(_options.RefreshTokenDays);
        if (expiresAtUtc > sessionExpiresAtUtc)
            expiresAtUtc = sessionExpiresAtUtc;

        return (rawToken, new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            FamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            SessionExpiresAtUtc = sessionExpiresAtUtc,
            CreatedByIp = NormalizeIp(ipAddress)
        });
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc,
        string? ipAddress, string reason, CancellationToken cancellationToken)
    {
        var normalizedIp = NormalizeIp(ipAddress);
        await _context.RefreshTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAtUtc, revokedAtUtc)
                .SetProperty(x => x.RevokedByIp, normalizedIp)
                .SetProperty(x => x.RevocationReason, reason), cancellationToken);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? NormalizeIp(string? ipAddress) =>
        string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress[..Math.Min(ipAddress.Length, 45)];
}
