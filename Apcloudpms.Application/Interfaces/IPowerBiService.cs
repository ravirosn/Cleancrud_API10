using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IPowerBiService
{
    Task<PowerBiEmbedConfigDto> GetEmbedConfigAsync(CancellationToken cancellationToken);
}
