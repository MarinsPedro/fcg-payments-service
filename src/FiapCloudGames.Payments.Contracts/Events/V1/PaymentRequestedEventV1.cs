namespace FiapCloudGames.Payments.Contracts.Events.V1;
public sealed record PaymentRequestedEventV1(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string IdempotencyKey,
    DateTimeOffset OccurredAtUtc);