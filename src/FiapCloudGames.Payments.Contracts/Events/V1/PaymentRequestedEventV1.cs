using MassTransit;

namespace FiapCloudGames.Payments.Contracts.Events.V1;

[MessageUrn("PaymentRequestedEventV1", useDefaultPrefix: true)]
public sealed record PaymentRequestedEventV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string IdempotencyKey,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
