namespace FiapCloudGames.Payments.Application.Features.Payments.Common;

public static class PaymentIdempotencyKey
{
    public static string ForOrder(Guid orderId) =>
        $"payment:{orderId:N}";
}
