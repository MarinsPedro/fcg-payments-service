using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Domain.Outbox;
using FiapCloudGames.Payments.Domain.Payments;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(
    DbContextOptions<PaymentsDbContext> options)
    : DbContext(options), IPaymentsUnitOfWork
{
    public DbSet<Payment> Payments => Set<Payment>();

    // Outbox manual - será removido depois da troca do fluxo de publicação
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaymentsDbContext).Assembly);

        modelBuilder.AddInboxStateEntity(
            entity => entity.ToTable("inbox_state"));

        modelBuilder.AddOutboxMessageEntity(
            entity => entity.ToTable("outbox_message"));

        modelBuilder.AddOutboxStateEntity(
            entity => entity.ToTable("outbox_state"));
    }
}