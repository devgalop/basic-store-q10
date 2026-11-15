using Store.Core.Constants;
using Store.Core.Enums.Stock;

namespace Store.Core.Events;

/// <summary>
/// Evento publicado cuando el inventario no puede reservar el stock solicitado para un pedido.
/// </summary>
/// <param name="EventId">Identificador único del evento.</param>
/// <param name="OrderId">Identificador del pedido.</param>
/// <param name="Sku">SKU del producto.</param>
/// <param name="Quantity">Cantidad solicitada del producto.</param>
/// <param name="Reason">Razón por la cual se rechazó el stock.</param>
/// <param name="OccurredAt">Fecha y hora en que ocurrió el evento.</param>
public sealed record StockRejected(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Quantity,
    EStockRejectionReason Reason,
    DateTime OccurredAt) : IIntegrationEvent
{
    /// <summary>
    /// Tipo del evento. Describe la clase del evento, no la instancia.
    /// </summary>
    public static string EventType => EventTypeConstants.ORDER_REJECTED;
    /// <summary>
    /// Versión del contrato del evento. Cambia cuando se introduce un cambio incompatible.
    /// </summary>
    public static int Version => 1;
}
