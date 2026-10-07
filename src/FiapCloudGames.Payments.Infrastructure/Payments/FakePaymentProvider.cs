using FiapCloudGames.Payments.Application.Abstractions.Payments;
using Microsoft.Extensions.Configuration;

namespace FiapCloudGames.Payments.Infrastructure.Payments;

internal sealed class FakePaymentProvider : IPaymentProvider
{
    private readonly int _approvalRate;

    public FakePaymentProvider(IConfiguration configuration)
    {
        var configuredRate = configuration["FakePaymentProvider:ApprovalRate"];

        _approvalRate =
            int.TryParse(configuredRate, out var rate)
                ? Math.Clamp(rate, 0, 100)
                : 90;
    }

    public string Name => "Fake";

    public Task<PaymentProviderResult> ChargeAsync(
        Guid paymentId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var approved = Random.Shared.Next(100) < _approvalRate;

        if (!approved)
        {
            return Task.FromResult(
                new PaymentProviderResult(
                    false,
                    null,
                    "Pagamento recusado pelo FakePaymentProvider."));
        }

        var transactionId = $"fake_{paymentId:N}";

        return Task.FromResult(
            new PaymentProviderResult(
                true,
                transactionId,
                null));
    }
}