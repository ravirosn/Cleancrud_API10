using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IPermitApplicationGuidelineService
{
    Task<PermitApplicationGuidelinePagedResponseDto> GetPagedAsync(
        PermitApplicationGuidelineQueryDto query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermitTypeGuidelineOptionDto>> GetPermitTypesAsync(
        CancellationToken cancellationToken = default);
    Task<PermitApplicationGuidelineDto?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);
    Task<PermitApplicationGuidelineDto> CreateAsync(
        PermitApplicationGuidelineRequestDto request, CancellationToken cancellationToken = default);
    Task<PermitApplicationGuidelineDto?> UpdateAsync(
        int id, PermitApplicationGuidelineRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
