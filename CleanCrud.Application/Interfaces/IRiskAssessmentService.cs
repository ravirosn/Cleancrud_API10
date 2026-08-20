using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

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

    Task<RiskAssessmentWriteResult> CreateAsync(
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<RiskAssessmentWriteResult> UpdateAsync(
        int riskAssessmentId,
        RiskAssessmentRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);
}
