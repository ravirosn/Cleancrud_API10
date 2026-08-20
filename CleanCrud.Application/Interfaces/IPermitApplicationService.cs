using CleanCrud.Application.DTOs;

namespace CleanCrud.Application.Interfaces;

public interface IPermitApplicationService
{
    Task<PermitApplicationDetailsDto?> GetByIdAsync(
        long permitApplicationId,
        CancellationToken cancellationToken = default);

    Task<PermitApplicationPagedResponseDto> GetByCreatedUserAsync(
        int userId,
        PermitApplicationQueryDto query,
        CancellationToken cancellationToken = default);

    Task<PermitApplicationActionResponseDto?> CompleteAsync(
        long permitApplicationId,
        string? remarks,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PermitApplicationActionResponseDto?> CancelAsync(
        long permitApplicationId,
        string? remarks,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PermitApplicationUpdateResult> UpdateAsync(
        long permitApplicationId,
        PermitApplicationUpdateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PermitApplicationUpdateResult> UpdateAndFinalizeAsync(
        long permitApplicationId,
        PermitApplicationUpdateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);
}
