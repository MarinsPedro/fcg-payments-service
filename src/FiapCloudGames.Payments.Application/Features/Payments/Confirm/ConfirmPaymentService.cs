using System.Text.Json;
using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Outbox;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Confirm;

public sealed class ConfirmPaymentService(
    IPaymentRepository payments,
    IOutboxRepository outbox,
    IPaymentsUnitOfWork unitOfWork,
    IPaymentProvider provider,
    ICorrelationContext correlation,
    TimeProvider clock)
{
    public async Task<PaymentResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Pagamento não encontrado.");

        // Já foi processado anteriormente.
        // Não chama o provider novamente.
        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed)
            return PaymentResult.From(payment);

        if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded)
            throw AppException.BusinessRule(
                "O estado atual do pagamento não permite processamento.");

        PaymentProviderResult result;

        try
        {
            result = await provider.ChargeAsync(
                payment.Id,
                payment.Amount,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Erro técnico não significa pagamento recusado.
            // Mantém Pending para permitir retry posteriormente.
            throw AppException.BusinessRule(
                "Não foi possível processar o pagamento no provider.");
        }

        var now = clock.GetUtcNow();

        if (!result.Success || string.IsNullOrWhiteSpace(result.TransactionId))
        {
            var failureReason =
                result.FailureReason
                ?? "Pagamento recusado pelo provider.";

            payment.MarkFailed(failureReason, now);

            var payload = JsonSerializer.Serialize(
                new PaymentFailedOutboxPayload(
                    payment.Id,
                    payment.OrderId,
                    payment.UserId,
                    payment.Amount,
                    failureReason,
                    now));

            await outbox.AddAsync(
                OutboxMessage.Create(
                    "PaymentFailedEvent",
                    payload,
                    correlation.CorrelationId,
                    now),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return PaymentResult.From(payment);
        }

        payment.MarkPaid(result.TransactionId, now);

        var paidPayload = JsonSerializer.Serialize(
            new PaymentPaidOutboxPayload(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.Amount,
                payment.ProviderTransactionId!,
                now));

        await outbox.AddAsync(
            OutboxMessage.Create(
                "PaymentPaidEvent",
                paidPayload,
                correlation.CorrelationId,
                now),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentResult.From(payment);
    }
}

public sealed record PaymentPaidOutboxPayload(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string ProviderTransactionId,
    DateTimeOffset OccurredAtUtc);

public sealed record PaymentFailedOutboxPayload(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string FailureReason,
    DateTimeOffset OccurredAtUtc);