
using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Contracts.Events.V1;
using FiapCloudGames.Payments.Domain.Payments;
using MassTransit;

namespace FiapCloudGames.Payments.Application.Features.Payments.Confirm;

public sealed class ConfirmPaymentService(
    IPaymentRepository payments,
    IPublishEndpoint publishEndpoint,
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

            await publishEndpoint.Publish(
                new PaymentFailedEventV1(
                    payment.Id,
                    payment.OrderId,
                    payment.UserId,
                    payment.Amount,
                    failureReason,
                    now),
                context =>
                {
                    ApplyCorrelationId(context, correlationId);
                },
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return PaymentResult.From(payment);
        }

        // Pagamento aprovado.
        payment.MarkPaid(result.TransactionId, now);

        await publishEndpoint.Publish(
            new PaymentPaidEventV1(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.Amount,
                payment.ProviderTransactionId!,
                now),
            context =>
            {
                ApplyCorrelationId(context, correlationId);
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PaymentResult.From(payment);
    }

    private static void ApplyCorrelationId(
        PublishContext context,
        string? correlationId)
    {
        if (Guid.TryParse(correlationId, out var parsed))
            {
                context.CorrelationId = parsed;
            }
    }
}
