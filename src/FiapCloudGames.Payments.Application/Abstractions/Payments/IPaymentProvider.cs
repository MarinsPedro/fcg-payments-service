namespace FiapCloudGames.Payments.Application.Abstractions.Payments;

public interface IPaymentProvider
{
    string Name { get; }
    Task<PaymentProviderResult> ChargeAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken);
}

public sealed record PaymentProviderResult(bool Success, string? TransactionId, string? FailureReason);
