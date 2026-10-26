using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.UnitTests.Domain;

public sealed class PaymentTests
{
    [Fact]
    public void Create_ShouldStartPending()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 59.90m, "Fake", "order-1", "corr-1", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(59.90m, payment.Amount);
        Assert.Equal("corr-1", payment.CorrelationId);
    }

    [Fact]
    public void MarkPaid_ShouldStoreTransactionId()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "Fake", "order-2", "corr-2", DateTimeOffset.UtcNow);
        payment.MarkPaid("tx-123", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("tx-123", payment.ProviderTransactionId);
    }

    [Fact]
    public void Cancel_AfterPaid_ShouldFail()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "Fake", "order-3", "corr-3", DateTimeOffset.UtcNow);
        payment.MarkPaid("tx-123", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => payment.Cancel(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkFailed_ShouldStoreFailureReason()
    {
        var payment = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10m,
            "Fake",
            "order-4",
            "corr-4",
            DateTimeOffset.UtcNow);

        payment.MarkFailed("Pagamento recusado.", DateTimeOffset.UtcNow);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Pagamento recusado.", payment.FailureReason);
        Assert.Null(payment.ProviderTransactionId);
    }
}
