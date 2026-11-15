using Store.Core.Entities;
using Store.Core.Enums.Orders;

namespace Store.WebApi.Orders;

/// <summary>
/// Representa la respuesta de un pedido en la API web. Contiene la información pública de un pedido, incluyendo su estado y fechas de creación y actualización.
/// </summary>
/// <param name="Id">El identificador único del pedido.</param>
/// <param name="CustomerName">El nombre del cliente que realizó el pedido.</param>
/// <param name="Sku">El SKU del producto solicitado.</param>
/// <param name="Quantity">La cantidad de productos solicitados.</param>
/// <param name="Status">El estado actual del pedido.</param>
/// <param name="CreatedAt">La fecha y hora de creación del pedido.</param>
/// <param name="UpdatedAt">La fecha y hora de la última actualización del pedido, si aplica.</param>
public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string Sku,
    int Quantity,
    EOrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>
    /// Crea una instancia de OrderResponse a partir de una entidad Order.
    /// </summary>
    /// <param name="order">La entidad Order a partir de la cual se crea la respuesta.</param>
    /// <returns>Una instancia de OrderResponse que representa la información pública del pedido.</returns>
    public static OrderResponse From(Order order) => new(
        order.Id, order.CustomerName, order.Sku, order.Quantity, order.Status, order.CreatedAt, order.UpdatedAt);
}
