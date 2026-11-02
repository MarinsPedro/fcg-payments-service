using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Payments.Application.Features.Payments.Create;
using FiapCloudGames.Payments.Contracts.Events.V1;
using MassTransit;

namespace FiapCloudGames.Payments.Worker;

public sealed class PaymentRequestedConsumer(
    CreatePaymentService createService,
    MessageCorrelationContext correlationContext,
    ILogger<PaymentRequestedConsumer> logger)
    : IConsumer<PaymentRequestedEventV1>
{
    public async Task Consume(
        ConsumeContext<PaymentRequestedEventV1> context)
    {
        var paymentRequested = context.Message;
        Validate(paymentRequested);

        correlationContext.Set(
            context.CorrelationId?.ToString() ?? paymentRequested.OrderId.ToString());

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

    private static void Validate(PaymentRequestedEventV1 message)
    {
        List<AppError> errors = [];

        if (message.EventId == Guid.Empty)
            errors.Add(new AppError("O identificador do evento é obrigatório.", "eventId"));

        if (message.OccurredAtUtc == default ||
            message.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add(new AppError(
                "A data de ocorrência do evento deve estar em UTC.",
                "occurredAtUtc"));
        }

        if (errors.Count > 0)
            throw AppException.Validation(errors);
    }
}
