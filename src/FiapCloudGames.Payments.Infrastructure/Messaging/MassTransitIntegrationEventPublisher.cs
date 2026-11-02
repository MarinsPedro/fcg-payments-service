using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Messaging;
using FiapCloudGames.Payments.Contracts;
using MassTransit;

namespace FiapCloudGames.Payments.Infrastructure.Messaging;

internal sealed class MassTransitIntegrationEventPublisher(
    IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(
        T integrationEvent,
        string? correlationId,
        CancellationToken cancellationToken)
        where T : class, IIntegrationEvent =>
        publishEndpoint.Publish(
            integrationEvent,
            context =>
            {
                context.MessageId = integrationEvent.EventId;

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    return;
                }

                context.Headers.Set(
                    CorrelationIdConstants.HeaderName,
                    correlationId);

                if (Guid.TryParse(correlationId, out var nativeCorrelationId))
                {
                    context.CorrelationId = nativeCorrelationId;
                }
            },
            cancellationToken);
}
