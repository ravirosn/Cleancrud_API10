using CleanCrud.Application.DTOs;
using CleanCrud.Domain.Entities;

namespace CleanCrud.Application.Interfaces;

public interface IJwtService
{
    AccessTokenDto GenerateAccessToken(User user, IEnumerable<string> roles);
}
