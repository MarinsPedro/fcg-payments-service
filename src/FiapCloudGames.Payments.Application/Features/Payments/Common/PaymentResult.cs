using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Common;

public sealed record PaymentResult(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentStatus Status,
    string Provider,
    string? ProviderTransactionId,
    string? FailureReason,
    string IdempotencyKey,
    string CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static PaymentResult From(Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.UserId,
        payment.Amount,
        payment.Status,
        payment.Provider,
        payment.ProviderTransactionId,
        payment.FailureReason,
        payment.IdempotencyKey,
        payment.CorrelationId,
        payment.CreatedAtUtc,
        payment.UpdatedAtUtc);
}
