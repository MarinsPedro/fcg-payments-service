namespace FiapCloudGames.Payments.Domain.Outbox;

public interface IOutboxRepository
{
    Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int take,
        CancellationToken cancellationToken);
}