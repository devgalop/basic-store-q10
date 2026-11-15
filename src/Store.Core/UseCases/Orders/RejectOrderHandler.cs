using Store.Core.Contracts.Messaging;
using Store.Core.Contracts.Orders;
using Store.Core.Enums.Orders;
using Store.Core.Events;
using Store.Core.Exceptions;

namespace Store.Core.UseCases.Orders;

/// <summary>
/// Reacciona a StockRejected: el pedido pasa de Pending a Rejected. Es el desenlace de una regla de negocio
/// (stock insuficiente), no de un fallo técnico: los fallos técnicos nunca llegan hasta acá como rechazo.
///
/// Mismo criterio que ConfirmOrderHandler: ya Rejected = no-op; Confirmed = contradicción (lanza, va a la DLQ).
/// </summary>
public sealed class RejectOrderHandler(
    IOrderStore store, 
    TimeProvider clock
) : IIntegrationEventHandler<StockRejected>
{
    /// <summary>
    /// Maneja el evento StockRejected rechazando el pedido correspondiente.
    /// </summary>
    /// <param name="event">Evento StockRejected que contiene la información del pedido a rechazar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    /// <exception cref="OrderNotFoundException">Se lanza si el pedido correspondiente al evento no se encuentra.</exception>
    public async Task HandleAsync(
        StockRejected @event, 
        CancellationToken cancellationToken
    )
    {
        var order = await store.FindTrackedAsync(@event.OrderId, cancellationToken)
                    ?? throw new OrderNotFoundException(@event.OrderId);

        if (order.Status == EOrderStatus.Rejected) return;

        order.Reject(clock.GetUtcNow().UtcDateTime);
    }
}
