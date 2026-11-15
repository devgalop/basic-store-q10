using Store.Core.Events;

namespace Store.Core.Contracts.Messaging;

/// <summary>
/// Representa un outbox de eventos de integración, que permite registrar eventos para su publicación posterior.
/// </summary>
public interface IEventOutbox
{
    /// <summary>
    /// Registra un evento de integración para su publicación posterior.
    /// </summary>
    /// <typeparam name="TEvent">Tipo del evento de integración.</typeparam>
    /// <param name="event">Evento de integración a registrar.</param>
    void Add<TEvent>(TEvent @event) where TEvent : IIntegrationEvent;
}
