using Apcloudpms.Application.DTOs;

namespace Apcloudpms.Application.Interfaces;

public interface IRiskAssessmentService
{
    Task<RiskAssessmentPagedResponseDto> GetPagedAsync(
        RiskAssessmentQueryDto query,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentDetailsDto?> GetByIdAsync(
        int riskAssessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAssessmentPermitApplicationDto>> GetPermitApplicationsAsync(
        int riskAssessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RiskAssessmentUserOptionDto>> GetUserOptionsAsync(
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentWriteResult> CreateAsync(
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentWriteResult> UpdateAsync(
        int riskAssessmentId,
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentWriteResult> ContinueCreateAsync(
        int riskAssessmentId,
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);
}
