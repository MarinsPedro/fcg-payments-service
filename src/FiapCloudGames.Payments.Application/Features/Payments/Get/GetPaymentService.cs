using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Domain.Payments;

namespace FiapCloudGames.Payments.Application.Features.Payments.Get;

public sealed class GetPaymentService(IPaymentRepository payments)
{
    public async Task<PaymentResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Pagamento não encontrado.");
        return PaymentResult.From(payment);
    }

    public async Task<PaymentResult> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw AppException.NotFound("Pagamento não encontrado para o pedido informado.");
        return PaymentResult.From(payment);
    }
}
