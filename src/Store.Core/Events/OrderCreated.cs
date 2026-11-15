using Store.Core.Constants;

namespace Store.Core.Events;

/// <summary>
/// Evento publicado cuando se crea un pedido. Contiene la información necesaria para que el inventario pueda reservar el stock correspondiente.
/// </summary>
/// <param name="EventId">Identificador único del evento.</param>
/// <param name="OrderId">Identificador del pedido.</param>
/// <param name="Sku">SKU del producto.</param>
/// <param name="Quantity">Cantidad solicitada del producto.</param>
/// <param name="OccurredAt">Fecha y hora en que ocurrió el evento.</param>
public sealed record OrderCreated(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Quantity,
    DateTime OccurredAt) : IIntegrationEvent
{
    /// <summary>
    /// Tipo del evento. Describe la clase del evento, no la instancia.
    /// </summary>
    public static string EventType => EventTypeConstants.ORDER_CREATED;
    /// <summary>
    /// Versión del contrato del evento. Cambia cuando se introduce un cambio incompatible.
    /// </summary>
    public static int Version => 1;
}
