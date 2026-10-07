namespace FiapCloudGames.Payments.Domain.Payments;

public enum PaymentStatus
{
    Pending = 1,
    Authorized = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6
}
