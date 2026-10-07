using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.UnitTests.Domain;

public sealed class PaymentTests
{
    [Fact]
    public void Create_ShouldStartPending()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 59.90m, "Fake", "order-1", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(59.90m, payment.Amount);
    }

    [Fact]
    public void MarkPaid_ShouldStoreTransactionId()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "Fake", "order-2", DateTimeOffset.UtcNow);
        payment.MarkPaid("tx-123", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal("tx-123", payment.ProviderTransactionId);
    }

    [Fact]
    public void Cancel_AfterPaid_ShouldFail()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 10m, "Fake", "order-3", DateTimeOffset.UtcNow);
        payment.MarkPaid("tx-123", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => payment.Cancel(DateTimeOffset.UtcNow));
    }
}
