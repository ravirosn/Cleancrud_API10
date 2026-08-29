using Apcloud.Contracts.Authentication;

namespace Apcloud.Web.Services.Authentication;

public interface IAuthApiClient
{
    Task<AuthResponseDto> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<CurrentUserDetailsDto> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
