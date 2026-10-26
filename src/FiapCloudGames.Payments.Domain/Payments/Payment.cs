namespace FiapCloudGames.Payments.Domain.Payments;

public sealed class Payment
{
    private Payment() { }

    private Payment(
        Guid id,
        Guid orderId,
        Guid userId,
        decimal amount,
        string provider,
        string idempotencyKey,
        string correlationId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("O id do pagamento é obrigatório.", nameof(id));
        if (orderId == Guid.Empty) throw new ArgumentException("O orderId é obrigatório.", nameof(orderId));
        if (userId == Guid.Empty) throw new ArgumentException("O userId é obrigatório.", nameof(userId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "O valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("O provider é obrigatório.", nameof(provider));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("A chave de idempotência é obrigatória.", nameof(idempotencyKey));

        Id = id;
        OrderId = orderId;
        UserId = userId;
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Provider = provider.Trim();
        IdempotencyKey = idempotencyKey.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId.Trim();
        Status = PaymentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Payment Create(
        Guid orderId,
        Guid userId,
        decimal amount,
        string provider,
        string idempotencyKey,
        string correlationId,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), orderId, userId, amount, provider, idempotencyKey, correlationId, now);

    public void MarkPaid(string providerTransactionId, DateTimeOffset now)
    {
        if (Status == PaymentStatus.Paid) return;
        if (Status is PaymentStatus.Cancelled or PaymentStatus.Refunded)
            throw new InvalidOperationException("Um pagamento cancelado ou reembolsado não pode ser confirmado.");
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            throw new ArgumentException("A transação do provider é obrigatória.", nameof(providerTransactionId));

        ProviderTransactionId = providerTransactionId.Trim();
        FailureReason = null;
        Status = PaymentStatus.Paid;
        UpdatedAtUtc = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            throw new InvalidOperationException("O estado atual não permite marcar o pagamento como falho.");

        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Falha não informada." : reason.Trim();
        Status = PaymentStatus.Failed;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Cancelled) return;
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new InvalidOperationException("Um pagamento pago ou reembolsado não pode ser cancelado.");

        Status = PaymentStatus.Cancelled;
        UpdatedAtUtc = now;
    }
}
