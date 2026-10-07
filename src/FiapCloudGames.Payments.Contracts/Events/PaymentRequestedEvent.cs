namespace FiapCloudGames.Payments.Contracts.Events;

public sealed record PaymentRequestedEvent(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string IdempotencyKey,
    DateTimeOffset OccurredAtUtc);