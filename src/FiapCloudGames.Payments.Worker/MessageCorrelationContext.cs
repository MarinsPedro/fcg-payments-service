using FiapCloudGames.BuildingBlocks.Observability;

namespace FiapCloudGames.Payments.Worker;

public sealed class MessageCorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; private set; } = string.Empty;

    public void Set(string? correlationId)
    {
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId.Trim();
    }
}