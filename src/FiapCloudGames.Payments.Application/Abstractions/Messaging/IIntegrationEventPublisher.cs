using FiapCloudGames.Payments.Contracts;

namespace FiapCloudGames.Payments.Application.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(
        T integrationEvent,
        string? correlationId,
        CancellationToken cancellationToken)
        where T : class, IIntegrationEvent;
}
