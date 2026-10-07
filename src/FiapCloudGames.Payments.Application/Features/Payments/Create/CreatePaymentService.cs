using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Payments.Application.Abstractions.Payments;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Create;

public sealed class CreatePaymentService(
    IPaymentRepository payments,
    IPaymentsUnitOfWork unitOfWork,
    IPaymentProvider provider,
    TimeProvider clock)
{
    public async Task<PaymentResult> ExecuteAsync(CreatePaymentInput input, CancellationToken cancellationToken)
    {
        if (input.OrderId == Guid.Empty) throw AppException.Validation("orderId é obrigatório.");
        if (input.UserId == Guid.Empty) throw AppException.Validation("userId é obrigatório.");
        if (input.Amount <= 0) throw AppException.Validation("amount deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey)) throw AppException.Validation("idempotencyKey é obrigatório.");
        if (input.IdempotencyKey.Length > 128) throw AppException.Validation("idempotencyKey deve possuir no máximo 128 caracteres.");

        var existing = await payments.GetByIdempotencyKeyAsync(input.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderId != input.OrderId || existing.UserId != input.UserId || existing.Amount != decimal.Round(input.Amount, 2, MidpointRounding.ToEven))
                throw AppException.Conflict("A chave de idempotência já foi utilizada com dados diferentes.");
            return PaymentResult.From(existing);
        }

        var sameOrder = await payments.GetByOrderIdAsync(input.OrderId, cancellationToken);
        if (sameOrder is not null)
            throw AppException.Conflict("Já existe um pagamento para o pedido informado.");

        var payment = Payment.Create(input.OrderId, input.UserId, input.Amount, provider.Name, input.IdempotencyKey, clock.GetUtcNow());
        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return PaymentResult.From(payment);
    }
}
