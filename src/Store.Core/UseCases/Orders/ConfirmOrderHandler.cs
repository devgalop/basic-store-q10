using Store.Core.Contracts.Messaging;
using Store.Core.Contracts.Orders;
using Store.Core.Enums.Orders;
using Store.Core.Events;
using Store.Core.Exceptions;

namespace Store.Core.UseCases.Orders;

/// <summary>
/// Reacciona a StockReserved: el pedido pasa de Pending a Confirmed.
///
/// - Pedido ya Confirmed: no hace nada (resultado ya aplicado; el Inbox cubre el mismo EventId, esto cubre
///   un resultado repetido con otro EventId).
/// - Pedido Rejected: el stock se descontó pero el pedido figura rechazado. Es una contradicción: la
///   máquina de estados lanza y el mensaje termina en la DLQ. No se tapa.
/// - Pedido inexistente: lanza (ver OrderNotFoundException).
///
/// Se ejecuta dentro de la transacción del Inbox: la transición y el "ya procesado" se confirman juntos.
/// </summary>
public sealed class ConfirmOrderHandler(
    IOrderStore store, 
    TimeProvider clock
) : IIntegrationEventHandler<StockReserved>
{
    /// <summary>
    /// Maneja el evento StockReserved confirmando el pedido correspondiente.
    /// </summary>
    /// <param name="event">Evento StockReserved que contiene la información del pedido a confirmar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    /// <exception cref="OrderNotFoundException">Se lanza si el pedido correspondiente al evento no se encuentra.</exception>
    public async Task HandleAsync(
        StockReserved @event, 
        CancellationToken cancellationToken
    )
    {
        var order = await store.FindTrackedAsync(@event.OrderId, cancellationToken)
                    ?? throw new OrderNotFoundException(@event.OrderId);

        if (order.Status == EOrderStatus.Confirmed) return;

        order.Confirm(clock.GetUtcNow().UtcDateTime);
    }
}
