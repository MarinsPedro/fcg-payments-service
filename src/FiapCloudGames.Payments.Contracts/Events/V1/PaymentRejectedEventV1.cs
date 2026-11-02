using MassTransit;

namespace FiapCloudGames.Payments.Contracts.Events.V1;

[MessageUrn("PaymentRejectedEventV1", useDefaultPrefix: true)]
public sealed record PaymentRejectedEventV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string RejectionReason,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
