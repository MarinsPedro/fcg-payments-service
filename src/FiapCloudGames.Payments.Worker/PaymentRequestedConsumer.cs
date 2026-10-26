using FiapCloudGames.Payments.Application.Features.Payments.Create;
using FiapCloudGames.Payments.Contracts.Events;
using MassTransit;

namespace FiapCloudGames.Payments.Worker;

public sealed class PaymentRequestedConsumer(
    CreatePaymentService createService,
    MessageCorrelationContext correlationContext,
    ILogger<PaymentRequestedConsumer> logger)
    : IConsumer<PaymentRequestedEvent>
{
    public async Task Consume(
        ConsumeContext<PaymentRequestedEvent> context)
    {
        var paymentRequested = context.Message;

        correlationContext.Set(
            context.CorrelationId?.ToString());

        logger.LogInformation(
            "Registrando intenção de pagamento. " +
            "OrderId={OrderId}, MessageId={MessageId}, CorrelationId={CorrelationId}.",
            paymentRequested.OrderId,
            context.MessageId,
            correlationContext.CorrelationId);

        var payment =
            await createService.ExecuteAsync(
                new CreatePaymentInput(
                    paymentRequested.OrderId,
                    paymentRequested.UserId,
                    paymentRequested.Amount,
                    paymentRequested.IdempotencyKey),
                context.CancellationToken);

        logger.LogInformation(
            "Intenção de pagamento registrada. " +
            "PaymentId={PaymentId}, Status={Status}. " +
            "O processamento ocorrerá posteriormente pela API HTTP.",
            payment.Id,
            payment.Status);
    }
}