namespace Store.Core.Events;

/// <summary>
/// Contrato común de los eventos que cruzan el broker.
/// EventType y Version son estáticos: describen al TIPO, no a la instancia.
/// Viajan como metadata del mensaje (headers) y sirven de routing key.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Tipo del evento. Describe la clase del evento, no la instancia.
    /// </summary>
    static abstract string EventType { get; }
    /// <summary>
    /// Versión del contrato del evento. Cambia cuando se introduce un cambio incompatible.
    /// </summary>
    static abstract int Version { get; }

    /// <summary>
    /// Identificador único: es la clave de idempotencia del Inbox.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Identificador del pedido; también actúa como correlation id del flujo.
    /// </summary>
    Guid OrderId { get; }

    /// <summary>
    /// Fecha y hora en que ocurrió el evento.
    /// </summary>
    DateTime OccurredAt { get; }
}
