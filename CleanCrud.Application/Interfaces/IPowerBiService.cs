using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IPowerBiService
{
    Task<PowerBiEmbedConfigDto> GetEmbedConfigAsync(CancellationToken cancellationToken);
}
