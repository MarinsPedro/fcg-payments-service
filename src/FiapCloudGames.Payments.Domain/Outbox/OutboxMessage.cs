namespace FiapCloudGames.Payments.Domain.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    private OutboxMessage(
        Guid id,
        string eventType,
        string payload,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(
        string eventType,
        string payload,
        string correlationId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException(
                "eventType é obrigatório.",
                nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException(
                "payload é obrigatório.",
                nameof(payload));

        return new OutboxMessage(
            Guid.NewGuid(),
            eventType.Trim(),
            payload,
            correlationId?.Trim() ?? string.Empty,
            now);
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        ProcessedAtUtc = null;

        Error = string.IsNullOrWhiteSpace(error)
            ? "Erro desconhecido ao publicar mensagem."
            : error.Length <= 1000
                ? error
                : error[..1000];
    }
}