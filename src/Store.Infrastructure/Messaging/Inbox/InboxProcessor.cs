using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.Infrastructure.Persistence.Common;

namespace Store.Infrastructure.Messaging.Inbox;

/// <summary>
/// Núcleo transaccional del consumidor idempotente, separado del transporte (RabbitMQ).
/// El consumidor se ocupa de recibir, parsear, hacer ack/retry/DLQ; este procesador se ocupa de que
/// el efecto de negocio ocurra EXACTAMENTE una vez por EventId. Al no depender del broker, se puede
/// ejercitar en los tests por el mismo camino que usa el consumidor real.
///
/// En UNA transacción de PostgreSQL:
///   1. INSERT en processed_events ... ON CONFLICT DO NOTHING   (Inbox)
///   2. el handler de negocio (mismo DbContext del scope = misma transacción)
///   3. SaveChanges + COMMIT
/// </summary>
/// <param name="scopes">Fábrica de scopes: cada evento usa su propio DbContext y su propia transacción.</param>
/// <param name="clock">Reloj para la fecha de procesamiento.</param>
internal sealed class InboxProcessor<TContext>(IServiceScopeFactory scopes, TimeProvider clock)
    where TContext : DbContext
{
    /// <summary>
    /// Procesa el evento dentro de una transacción de base de datos. Retorna true si se aplicó; false si era un duplicado (no se ejecutó nada).
    /// </summary>
    /// <param name="envelope">El sobre que contiene el evento a procesar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>true si el evento se procesó; false si era un duplicado.</returns>
    public async Task<bool> ProcessAsync(
        EventEnvelope envelope, 
        CancellationToken cancellationToken
    )
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var table = TrustedSql.Table<ProcessedEvent>(db);
        
        var inserted = await TrustedSql.ExecuteAsync(
            db,
            $$"""
              INSERT INTO {{table}} (event_id, event_type, processed_at)
              VALUES ({0}, {1}, {2})
              ON CONFLICT (event_id) DO NOTHING
              """,
            [envelope.EventId, envelope.EventType, clock.GetUtcNow().UtcDateTime], cancellationToken);

        // 0 filas = ese evento ya se procesó (o lo está procesando otra instancia y ya confirmó).
        if (inserted == 0) return false;

        await envelope.HandleAsync(scope.ServiceProvider, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
