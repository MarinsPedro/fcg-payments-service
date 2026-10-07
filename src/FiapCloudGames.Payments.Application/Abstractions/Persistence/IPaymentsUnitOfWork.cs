namespace FiapCloudGames.Payments.Application.Abstractions.Persistence;

public interface IPaymentsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
