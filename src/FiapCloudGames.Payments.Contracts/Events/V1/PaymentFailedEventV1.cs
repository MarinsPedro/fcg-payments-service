
namespace FiapCloudGames.Payments.Contracts.Events.V1;

public sealed record PaymentFailedEventV1(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string FailureReason,
    DateTimeOffset OccurredAtUtc);
