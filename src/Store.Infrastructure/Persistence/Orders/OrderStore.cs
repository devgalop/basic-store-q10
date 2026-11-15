using Microsoft.EntityFrameworkCore;
using Store.Core.Contracts.Orders;
using Store.Core.Entities;
using Store.Core.Events;
using Store.Infrastructure.Persistence.Common;

namespace Store.Infrastructure.Persistence.Orders;

/// <summary>
/// Implementación de IOrderStore que utiliza OrdersDbContext para persistir pedidos y eventos de dominio.
/// Utiliza un solo contexto y una sola transacción para asegurar que los pedidos y los eventos se persistan de manera atómica.
/// </summary>
/// <param name="db">El contexto de base de datos utilizado para persistir pedidos y eventos.</param>
internal sealed class OrderStore(OrdersDbContext db) : IOrderStore
{
    /// <summary>
    /// Agrega un nuevo pedido y su evento de dominio asociado a la base de datos de manera atómica.
    /// </summary>
    /// <param name="order">El pedido a agregar.</param>
    /// <param name="event">El evento de dominio asociado al pedido.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async Task AddAsync(Order order, OrderCreated @event, CancellationToken cancellationToken)
    {
        db.Orders.Add(order);
        db.OutboxMessages.Add(OutboxMessage.From(@event));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Busca un pedido por su identificador.
    /// </summary>
    /// <param name="id">El identificador del pedido.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>El pedido si se encuentra, de lo contrario null.</returns>
    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <summary>
    /// Busca un pedido para modificarlo: queda seguido (tracked) por el DbContext del scope.
    /// </summary>
    /// <param name="id">El identificador del pedido.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>El pedido si se encuentra, de lo contrario null.</returns>
    public Task<Order?> FindTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <summary>
    /// Lista todos los pedidos en la base de datos, ordenados por fecha de creación descendente.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una lista de pedidos.</returns>
    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) =>
        await db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
}
