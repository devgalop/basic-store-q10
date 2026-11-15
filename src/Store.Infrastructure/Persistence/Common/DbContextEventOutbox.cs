using Microsoft.EntityFrameworkCore;
using Store.Core.Contracts.Messaging;
using Store.Core.Events;

namespace Store.Infrastructure.Persistence.Common;

/// <summary>
/// Implementa un outbox de eventos de integración utilizando un DbContext. Los eventos se agregan al DbContext y se persisten junto con los cambios de negocio en la misma transacción.
/// </summary>
/// <param name="db">El DbContext utilizado para persistir los eventos de integración.</param>
internal sealed class DbContextEventOutbox(DbContext db) : IEventOutbox
{
    /// <summary>
    /// Agrega un evento de integración al DbContext para su persistencia posterior.
    /// </summary>
    /// <typeparam name="TEvent">Tipo del evento de integración.</typeparam>
    /// <param name="event">Evento de integración a registrar.</param>
    public void Add<TEvent>(TEvent @event) where TEvent : IIntegrationEvent =>
        db.Set<OutboxMessage>().Add(OutboxMessage.From(@event));
}
