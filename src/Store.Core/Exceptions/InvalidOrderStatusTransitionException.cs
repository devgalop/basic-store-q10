using Store.Core.Enums.Orders;

namespace Store.Core.Exceptions;

/// <summary>
/// Excepción lanzada cuando se intenta una transición de estado de pedido no permitida.
/// </summary>
/// <param name="orderId">Identificador del pedido.</param>
/// <param name="from">Estado actual del pedido.</param>
/// <param name="to">Estado al que se intenta cambiar el pedido.</param>
public sealed class InvalidOrderStatusTransitionException(Guid orderId, EOrderStatus from, EOrderStatus to)
    : InvalidOperationException($"El pedido {orderId} no puede pasar de {from} a {to}.")
{
    /// <summary>
    /// Identificador del pedido que causó la excepción.
    /// </summary>
    public Guid OrderId { get; } = orderId;
    /// <summary>
    /// Estado actual del pedido que causó la excepción.
    /// </summary>
    public EOrderStatus From { get; } = from;
    /// <summary>
    /// Estado al que se intentó cambiar el pedido.
    /// </summary>
    public EOrderStatus To { get; } = to;
}
