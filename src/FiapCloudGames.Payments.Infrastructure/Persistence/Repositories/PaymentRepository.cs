using FiapCloudGames.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(PaymentsDbContext dbContext) : IPaymentRepository
{
    public Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        dbContext.Payments.AddAsync(payment, cancellationToken).AsTask();

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Payments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Payments.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Payments.SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
}
