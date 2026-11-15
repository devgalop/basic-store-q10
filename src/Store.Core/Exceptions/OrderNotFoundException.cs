namespace Store.Core.Exceptions;

/// <summary>
/// Llegó el resultado de una reserva para un pedido que no existe. No debería ocurrir: el pedido y su evento
/// OrderCreated se guardan en la misma transacción, así que el pedido siempre existe antes que cualquier resultado.
/// Si pasa, es una inconsistencia real: el mensaje se reintenta y termina en la DLQ para investigarlo.
/// </summary>
public sealed class OrderNotFoundException(Guid orderId)
    : InvalidOperationException($"No existe el pedido {orderId}.")
{
    public Guid OrderId { get; } = orderId;
}
