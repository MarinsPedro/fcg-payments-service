namespace FiapCloudGames.Payments.Contracts.Events.V1;

public sealed record PaymentPaidEventV1(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string ProviderTransactionId,
    DateTimeOffset OccurredAtUtc);