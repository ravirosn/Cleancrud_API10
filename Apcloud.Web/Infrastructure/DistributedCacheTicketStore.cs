using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace Apcloud.Web.Infrastructure;

/// <summary>
/// Keeps authentication tickets (including API tokens) out of the browser. The
/// cookie contains only an opaque, random lookup key.
/// </summary>
public sealed class DistributedCacheTicketStore(
    IDistributedCache cache,
    IDataProtectionProvider dataProtectionProvider) : ITicketStore
{
    private const string KeyPrefix = "Apcloud.Web.AuthTicket:";
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "Apcloud.Web",
        nameof(DistributedCacheTicketStore),
        "v1");

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid():N}";
        await StoreTicketAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => StoreTicketAsync(key, ticket);

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!IsValidKey(key))
        {
            return null;
        }

        var protectedTicket = await cache.GetAsync(key);
        if (protectedTicket is null)
        {
            return null;
        }

        try
        {
            var serializedTicket = _protector.Unprotect(protectedTicket);
            return TicketSerializer.Default.Deserialize(serializedTicket);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            await cache.RemoveAsync(key);
            return null;
        }
    }

    public Task RemoveAsync(string key) =>
        IsValidKey(key) ? cache.RemoveAsync(key) : Task.CompletedTask;

    private Task StoreTicketAsync(string key, AuthenticationTicket ticket)
    {
        if (!IsValidKey(key))
        {
            throw new ArgumentException("Invalid authentication ticket key.", nameof(key));
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = ticket.Properties.ExpiresUtc ?? now.Add(DefaultLifetime);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt > now ? expiresAt : now.AddMinutes(1)
        };

        var serializedTicket = TicketSerializer.Default.Serialize(ticket);
        return cache.SetAsync(key, _protector.Protect(serializedTicket), options);
    }

    private static bool IsValidKey(string? key) =>
        key is { Length: 55 } && key.StartsWith(KeyPrefix, StringComparison.Ordinal);
}
