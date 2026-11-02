using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Payments.Application.Features.Payments.Common;
using FiapCloudGames.Payments.Application.Features.Payments.Create;

namespace FiapCloudGames.Payments.Presentation.Features.Payments;

public sealed record CreatePaymentRequest(
    [param: Required] Guid OrderId,
    [param: Required] Guid UserId,

    [param: Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true)]
    decimal Amount)
{
    public CreatePaymentInput ToInput() =>
        new(
            OrderId,
            UserId,
            Amount,
            PaymentIdempotencyKey.ForOrder(OrderId));
}
