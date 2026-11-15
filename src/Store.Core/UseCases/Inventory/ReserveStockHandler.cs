using Store.Core.Contracts.Inventory;
using Store.Core.Contracts.Messaging;
using Store.Core.Enums.Stock;
using Store.Core.Events;

namespace Store.Core.UseCases.Inventory;

/// <summary>
/// Maneja la reserva de stock en respuesta a un evento OrderCreated.
/// </summary>
/// <param name="stock">Servicio para gestionar la reserva de stock.</param>
/// <param name="outbox">Servicio para publicar eventos de integración de manera confiable.</param>
/// <param name="clock">Proveedor de tiempo para obtener la fecha y hora actual.</param>
public sealed class ReserveStockHandler(
    IStockReservation stock,
    IEventOutbox outbox,
    TimeProvider clock
) : IIntegrationEventHandler<OrderCreated>
{
    /// <summary>
    /// Maneja el evento OrderCreated intentando reservar el stock correspondiente.
    /// </summary>
    /// <param name="event">Evento OrderCreated que contiene la información del pedido y la cantidad a reservar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si la cantidad del evento es menor o igual a cero.</exception>
    public async Task HandleAsync(OrderCreated @event, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(@event.Quantity);

        var reserved = await stock.TryReserveAsync(@event.Sku, @event.Quantity, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;

        if (!reserved)
        {
            outbox.Add(
                new StockRejected(
                    Guid.CreateVersion7(), 
                    @event.OrderId, 
                    @event.Sku, 
                    @event.Quantity,
                    EStockRejectionReason.InsufficientStock, 
                    now
                )
            );
            return;
        }
        
        outbox.Add(
            new StockReserved(
                Guid.CreateVersion7(), 
                @event.OrderId, 
                @event.Sku, 
                @event.Quantity, 
                now
            )
        );    
    }
}
