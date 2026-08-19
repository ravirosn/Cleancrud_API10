using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IRiskAssessmentService
{
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
