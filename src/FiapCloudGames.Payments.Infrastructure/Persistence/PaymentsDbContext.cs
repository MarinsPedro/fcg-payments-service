using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Domain.Outbox;
using FiapCloudGames.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options), IPaymentsUnitOfWork
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
