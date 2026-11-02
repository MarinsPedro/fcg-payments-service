using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Create;

public sealed class CreatePaymentService(
    IPaymentRepository payments,
    IPaymentsUnitOfWork unitOfWork,
    IPaymentProvider provider,
    ICorrelationContext correlation,
    TimeProvider clock)
{
    public async Task<PaymentResult> ExecuteAsync(CreatePaymentInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);

        var idempotencyKey = input.IdempotencyKey.Trim();

        var existing = await payments.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderId != input.OrderId || existing.UserId != input.UserId || existing.Amount != decimal.Round(input.Amount, 2, MidpointRounding.ToEven))
                throw AppException.Conflict("A chave de idempotência já foi utilizada com dados diferentes.");
            return PaymentResult.From(existing);
        }

        var sameOrder = await payments.GetByOrderIdAsync(input.OrderId, cancellationToken);
        if (sameOrder is not null)
            throw AppException.Conflict("Já existe um pagamento para o pedido informado.");

        var payment = Payment.Create(
            input.OrderId,
            input.UserId,
            input.Amount,
            provider.Name,
            idempotencyKey,
            correlation.CorrelationId,
            clock.GetUtcNow());
        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PaymentResult.From(payment);
    }

    private static void Validate(CreatePaymentInput input)
    {
        List<AppError> errors = [];

        if (input.OrderId == Guid.Empty)
            errors.Add(new AppError("O identificador do pedido é obrigatório.", "orderId"));

        if (input.UserId == Guid.Empty)
            errors.Add(new AppError("O identificador do usuário é obrigatório.", "userId"));

        if (input.Amount <= 0)
            errors.Add(new AppError("O valor deve ser maior que zero.", "amount"));

        if (string.IsNullOrWhiteSpace(input.IdempotencyKey))
        {
            errors.Add(new AppError("A chave de idempotência é obrigatória.", "idempotencyKey"));
        }
        else
        {
            var idempotencyKey = input.IdempotencyKey.Trim();
            var expectedKey = PaymentIdempotencyKey.ForOrder(input.OrderId);

            if (idempotencyKey.Length > 128)
                errors.Add(new AppError("A chave de idempotência deve possuir no máximo 128 caracteres.", "idempotencyKey"));

            if (input.OrderId != Guid.Empty &&
                !string.Equals(idempotencyKey, expectedKey, StringComparison.Ordinal))
            {
                errors.Add(new AppError(
                    "A chave de idempotência não corresponde ao pedido informado.",
                    "idempotencyKey"));
            }
        }

        if (errors.Count > 0)
            throw AppException.Validation(errors);
    }
}
