namespace Store.Core.Models.Orders;

/// <summary>
/// Representa una solicitud para crear un pedido.
/// Solo contiene datos que aporta el cliente: el identificador, el estado
/// y la fecha de creación los asigna el servidor.
/// </summary>
/// <param name="CustomerName">Nombre del cliente</param>
/// <param name="Sku">Código del producto</param>
/// <param name="Quantity">Cantidad del producto</param>
public record CreateOrderRequest
(
    string CustomerName,
    string Sku,
    int Quantity
);
