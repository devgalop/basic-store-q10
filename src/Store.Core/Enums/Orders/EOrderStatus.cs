namespace Store.Core.Enums.Orders;

/// <summary>
/// Representa los posibles estados de un pedido.
/// </summary>
public enum EOrderStatus
{
    /// <summary>
    /// Pedido creado, esperando el resultado de la reserva de stock.
    /// Si hay un fallo, el pedido permanece en este estado hasta que se pueda reintentar.
    /// </summary>
    Pending,
    /// <summary>
    /// Stock reservado. Estado final.
    /// </summary>
    Confirmed,
    /// <summary>
    /// Rechazado por stock insuficiente. Estado final.
    /// </summary>
    Rejected
}
