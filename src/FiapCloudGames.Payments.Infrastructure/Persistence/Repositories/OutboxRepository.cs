using FiapCloudGames.Payments.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Infrastructure.Persistence.Repositories;

internal sealed class OutboxRepository(
    PaymentsDbContext dbContext) : IOutboxRepository
{
    public Task AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken) =>
        dbContext.OutboxMessages
            .AddAsync(message, cancellationToken)
            .AsTask();

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int take,
        CancellationToken cancellationToken)
    {
        return await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}