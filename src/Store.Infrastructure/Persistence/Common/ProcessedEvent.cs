namespace Store.Infrastructure.Persistence.Common;

/// <summary>
/// Inbox: registro de eventos ya procesados. La PK sobre EventId es la que garantiza
/// la idempotencia: se inserta en la misma transacción que el efecto de negocio, así que
/// un duplicado viola la PK y la transacción completa se revierte.
/// </summary>
public sealed class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, string eventType, DateTime processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }
}
