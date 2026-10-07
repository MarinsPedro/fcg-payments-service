namespace FiapCloudGames.Payments.Application.Features.Payments.Create;

public sealed record CreatePaymentInput(Guid OrderId, Guid UserId, decimal Amount, string IdempotencyKey);
