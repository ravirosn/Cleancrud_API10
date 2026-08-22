using Apcloudpms.Application.DTOs;
using Apcloudpms.Domain.Entities;

namespace Apcloudpms.Application.Interfaces;

public interface IJwtService
{
    AccessTokenDto GenerateAccessToken(User user, IEnumerable<string> roles);
}
