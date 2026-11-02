using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Messaging;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Contracts.Events.V1;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Confirm;

public sealed class ConfirmPaymentService(
    IPaymentRepository payments,
    IIntegrationEventPublisher events,
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

        // Pagamento já processado.
        // Não chama o provider novamente.
        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed)
            return PaymentResult.From(payment);

        if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded)
            throw AppException.BusinessRule(
                "O estado atual do pagamento não permite processamento.");

        var result = await provider.ChargeAsync(
            payment.Id,
            payment.Amount,
            cancellationToken);

        var now = clock.GetUtcNow();

        var correlationId = string.IsNullOrWhiteSpace(payment.CorrelationId)
            ? correlation.CorrelationId
            : payment.CorrelationId;

        // Pagamento recusado ou sem transação válida.
        if (!result.Success || string.IsNullOrWhiteSpace(result.TransactionId))
        {
            var failureReason =
                result.FailureReason
                ?? "Pagamento recusado pelo provider.";

            payment.MarkFailed(failureReason, now);

            await events.PublishAsync(
                new PaymentRejectedEventV1(
                    Guid.NewGuid(),
                    payment.OrderId,
                    payment.UserId,
                    payment.Amount,
                    failureReason,
                    now),
                correlationId,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return PaymentResult.From(payment);
        }

        // Pagamento aprovado.
        payment.MarkPaid(result.TransactionId, now);

        await events.PublishAsync(
            new PaymentApprovedEventV1(
                Guid.NewGuid(),
                payment.OrderId,
                payment.UserId,
                payment.Amount,
                now),
            correlationId,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentResult.From(payment);
    }
}
