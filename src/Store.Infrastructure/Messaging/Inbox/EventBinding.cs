using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Store.Core.Contracts.Messaging;
using Store.Core.Events;
using Store.Infrastructure.Serialization;

namespace Store.Infrastructure.Messaging.Inbox;

/// <summary>
/// Representa el evento deserializado
/// </summary>
/// <param name="EventId">Identificador único del evento</param>
/// <param name="EventType">Tipo del evento</param>
/// <param name="HandleAsync">Acción de negocio a ejecutar para manejar el evento</param>
internal sealed record EventEnvelope(
    Guid EventId, string EventType, Func<IServiceProvider, CancellationToken, Task> HandleAsync);

/// <summary>
/// Representa un tipo de evento
/// </summary>
internal interface IEventBinding
{
    /// <summary>
    /// Version del evento
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Parsea el JSON del evento y lo convierte en un EventEnvelope.
    /// </summary>
    /// <param name="json">JSON del evento a parsear</param>
    /// <returns>EventEnvelope que representa el evento deserializado</returns>
    /// <exception cref="JsonException">El payload no es un evento válido: mensaje veneno.</exception>
    EventEnvelope Parse(string json);
}

/// <summary>
/// Une un tipo de evento con su handler correspondiente, sin que el consumidor conozca tipos concretos.
/// </summary>
/// <typeparam name="TEvent">Tipo de evento que implementa IIntegrationEvent</typeparam>
internal sealed class EventBinding<TEvent> : IEventBinding where TEvent : IIntegrationEvent
{
    public int Version => TEvent.Version;

    public EventEnvelope Parse(string json)
    {
        var @event = IntegrationEventSerializer.Deserialize<TEvent>(json);
        if (@event.EventId == Guid.Empty)
            throw new JsonException("El evento no trae un EventId válido.");

        return new EventEnvelope(
            @event.EventId,
            TEvent.EventType,
            (services, cancellationToken) =>
                services.GetRequiredService<IIntegrationEventHandler<TEvent>>().HandleAsync(@event, cancellationToken));
    }
}
