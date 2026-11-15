using Store.Core.Events;
using Store.Infrastructure.Serialization;

namespace Store.Infrastructure.Persistence.Common;

/// <summary>
/// Evento pendiente de publicar. Se inserta en la MISMA transacción que el cambio
/// de negocio (Transactional Outbox) y un publicador en segundo plano lo envía al broker.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Igual al EventId del evento: el consumidor deduplica con este valor.</summary>
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public int EventVersion { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    /// <summary>Null mientras esté pendiente.</summary>
    public DateTime? PublishedAt { get; private set; }

    /// <summary>Intentos de publicación fallidos; permite detectar mensajes "atascados".</summary>
    public int Attempts { get; private set; }

    /// <summary>Lease: mientras sea futuro, otra instancia del publicador no lo toma.</summary>
    public DateTime? LockedUntil { get; private set; }

    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage From<TEvent>(TEvent @event) where TEvent : IIntegrationEvent => new()
    {
        Id = @event.EventId,
        EventType = TEvent.EventType,
        EventVersion = TEvent.Version,
        Payload = IntegrationEventSerializer.Serialize(@event),
        OccurredAt = @event.OccurredAt,
    };
}
