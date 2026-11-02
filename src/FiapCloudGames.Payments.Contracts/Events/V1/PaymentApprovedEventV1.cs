using MassTransit;

namespace FiapCloudGames.Payments.Contracts.Events.V1;

[MessageUrn("PaymentApprovedEventV1", useDefaultPrefix: true)]
public sealed record PaymentApprovedEventV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
