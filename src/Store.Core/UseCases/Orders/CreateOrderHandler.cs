using Store.Core.Contracts.Orders;
using Store.Core.Entities;
using Store.Core.Events;
using Store.Core.Models.Orders;
using Store.Core.Validation.Orders;

namespace Store.Core.UseCases.Orders;

/// <summary>
/// Manejador del caso de uso de creación de pedidos. Valida la solicitud, crea el pedido en estado Pending y registra el evento OrderCreated.
/// </summary>
/// <param name="validator">El validador de la solicitud de creación de pedido.</param>
/// <param name="store">El almacén de pedidos donde se guardará el pedido creado.</param>
/// <param name="clock">El proveedor de tiempo para obtener la fecha y hora actual.</param>
public sealed class CreateOrderHandler(
    CreateOrderValidator validator,
    IOrderStore store,
    TimeProvider clock)
{
    /// <summary>
    /// Maneja la solicitud de creación de pedido. Valida la solicitud, crea el pedido en estado Pending y registra el evento OrderCreated.
    /// </summary>
    /// <param name="request">La solicitud de creación de pedido.</param>
    /// <param name="cancellationToken">El token de cancelación.</param>
    /// <returns>El resultado de la creación del pedido.</returns>
    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderRequest request, 
        CancellationToken cancellationToken
    )
    {
        var errors = await validator.ValidateAsync(request, cancellationToken);
        if (errors.Count > 0)
            return CreateOrderResult.Invalid(errors);

        var now = clock.GetUtcNow().UtcDateTime;
        var order = Order.Create(request.CustomerName, request.Sku, request.Quantity, now);
        var @event = new OrderCreated(Guid.CreateVersion7(), order.Id, order.Sku, order.Quantity, now);

        await store.AddAsync(order, @event, cancellationToken);

        return CreateOrderResult.Created(order);
    }
}
