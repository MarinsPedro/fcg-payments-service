using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Presentation.Features.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentStatus Status,
    string Provider,
    string? ProviderTransactionId,
    string? FailureReason,
    string IdempotencyKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static PaymentResponse From(PaymentResult result) => new(
        result.Id, result.OrderId, result.UserId, result.Amount, result.Status,
        result.Provider, result.ProviderTransactionId, result.FailureReason,
        result.IdempotencyKey, result.CreatedAtUtc, result.UpdatedAtUtc);
}
