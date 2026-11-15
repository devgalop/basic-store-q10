using Store.Core.Entities;
using Store.Core.Events;

namespace Store.Core.Contracts.Orders;

/// <summary>
/// Representa la abstracción de almacenamiento de pedidos.
/// </summary>
public interface IOrderStore
{
    /// <summary>
    /// Guarda el pedido y el evento OrderCreated de forma atómica.
    /// Evita dejar un pedido Pending para siempre o un evento huérfano.
    /// <param name="order">El pedido a guardar.</param>
    /// <param name="event">El evento OrderCreated asociado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// </summary>
    Task AddAsync(Order order, OrderCreated @event, CancellationToken cancellationToken);

    /// <summary>
    /// Busca un pedido por su identificador. Retorna null si no existe.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El pedido si existe; de lo contrario, null.</returns>
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Busca un pedido para modificarlo: queda seguido por la unidad de trabajo en curso, así que los cambios
    /// que se le hagan los persiste el SaveChanges del consumidor, dentro de su transacción.
    /// (FindAsync, en cambio, es solo lectura.)
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El pedido si existe; de lo contrario, null.</returns>
    Task<Order?> FindTrackedAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista todos los pedidos del más nuevo al más viejo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de pedidos.</returns>
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken);
}
