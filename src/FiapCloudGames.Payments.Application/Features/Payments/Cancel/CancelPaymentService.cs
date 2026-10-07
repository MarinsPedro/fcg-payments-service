using System.Text.Json;
using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Outbox;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Cancel;

public sealed class CancelPaymentService(
    IPaymentRepository payments,
    IOutboxRepository outbox,
    IPaymentsUnitOfWork unitOfWork,
    ICorrelationContext correlation,
    TimeProvider clock)
{
    public async Task<PaymentResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Pagamento não encontrado.");

        if (payment.Status == PaymentStatus.Cancelled) return PaymentResult.From(payment);

        try
        {
            payment.Cancel(clock.GetUtcNow());
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.BusinessRule(ex.Message);
        }

        var now = clock.GetUtcNow();
        var payload = JsonSerializer.Serialize(new PaymentCancelledOutboxPayload(payment.Id, payment.OrderId, now));
        await outbox.AddAsync(OutboxMessage.Create("PaymentCancelledEvent", payload, correlation.CorrelationId, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PaymentResult.From(payment);
    }
}

public sealed record PaymentCancelledOutboxPayload(Guid PaymentId, Guid OrderId, DateTimeOffset OccurredAtUtc);
