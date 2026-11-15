using Store.Core.Events;

namespace Store.Core.Contracts.Messaging;

/// <summary>
/// Lógica de negocio que reacciona a un evento de integración.
///
/// El consumidor ejecuta el handler DENTRO de una transacción de base de datos que ya incluye
/// el registro en el Inbox, y hace commit solo si el handler termina sin excepción. Por eso el
/// handler debe usar el DbContext del scope (el mismo de la transacción) y no abrir conexiones propias.
/// Una excepción significa "fallo técnico": el mensaje se reintenta; nunca debe usarse para
/// expresar un rechazo de negocio (eso se modela como resultado, p. ej. StockRejected).
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
