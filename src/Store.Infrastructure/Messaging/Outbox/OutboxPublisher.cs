using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;
using Store.Infrastructure.Persistence.Common;

namespace Store.Infrastructure.Messaging.Outbox;

/// <summary>
/// Lee la tabla outbox del servicio y publica los eventos pendientes en RabbitMQ.
/// Hospedado como servicio propio: no comparte ciclo de vida con ningún consumidor.
///
/// Garantías:
/// - AT-LEAST-ONCE: se marca como publicado DESPUÉS del confirm del broker. Si el proceso muere entre
///   ambos pasos, el evento se vuelve a publicar; el Inbox del consumidor absorbe el duplicado.
/// - VARIAS INSTANCIAS: el lote se reclama con FOR UPDATE SKIP LOCKED + un lease (locked_until).
///   Una instancia nunca espera a otra y ninguna publica lo que otra tiene reclamado.
/// - SIN TRANSACCIONES LARGAS: reclamar es UNA sentencia autocommit; publicar ocurre sin transacción
///   ni locks de fila abiertos. Si el lease vence (instancia caída), otra toma el evento.
/// - BROKER CAÍDO: el evento queda en la tabla; cada fallo suma un intento y retrasa el siguiente
///   con backoff exponencial (tope 60 s). Nunca se descarta: al volver el broker, se publica.
///
/// Monitoreo: pendientes = published_at IS NULL; atascados = attempts alto / last_error no nulo.
/// </summary>
public sealed class OutboxPublisher<TContext>(
    IServiceScopeFactory scopes,
    RabbitMqPublisher publisher,
    ILogger<OutboxPublisher<TContext>> logger
) : BackgroundService where TContext : DbContext
{
    private const int BATCH_SIZE = 50;
    private const int LEASE_SECONDS = 30;
    private readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Representa un mensaje reclamado de la tabla outbox que está listo para ser publicado en RabbitMQ.
    /// </summary>
    /// <param name="Id">El identificador único del mensaje en la tabla outbox.</param>
    /// <param name="EventType">El tipo de evento que representa el mensaje.</param>
    /// <param name="EventVersion">La versión del evento.</param>
    /// <param name="Payload">El contenido del mensaje en formato JSON.</param>
    /// <param name="Attempts">El número de intentos fallidos de publicación.</param>
    private sealed record ClaimedMessage(
        Guid Id, 
        string EventType, 
        int EventVersion, 
        string Payload, 
        int Attempts
    );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Publicador de outbox ({Context}) iniciado.", typeof(TContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimed = 0;
            try
            {
                claimed = await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Fallo al reclamar (p. ej. base caída): se reintenta en el próximo ciclo.
                logger.LogError(ex, "Falló el ciclo del publicador de outbox ({Context}).", typeof(TContext).Name);
            }

            // Lote completo = probablemente hay más: seguir sin esperar.
            if (claimed < BATCH_SIZE)
            {
                try { await Task.Delay(PollInterval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    /// <summary>
    /// Publica un lote de mensajes pendientes desde la tabla outbox.
    /// </summary>
    /// <param name="cancellationToken">El token de cancelación que permite abortar la operación.</param>
    /// <returns>El número de mensajes publicados en este lote.</returns>
    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var table = TrustedSql.Table<OutboxMessage>(db);

        // Reclama un lote de mensajes pendientes para su publicación.
        var batch = await ClaimAsync(db, table, cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                await publisher.PublishAsync(
                    RabbitMqTopology.Exchange,
                    routingKey: message.EventType,
                    Encoding.UTF8.GetBytes(message.Payload),
                    BuildProperties(message),
                    cancellationToken);

                await TrustedSql.ExecuteAsync(
                    db,
                    $"UPDATE {table} SET published_at = now(), locked_until = NULL, last_error = NULL WHERE id = {{0}}",
                    [message.Id], cancellationToken);

                logger.LogInformation("Evento {EventType} {EventId} publicado (intento {Attempt}).",
                    message.EventType, message.Id, message.Attempts + 1);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // el lease vence solo; otra instancia (o esta, al reiniciar) lo retoma.
            }
            catch (Exception ex)
            {
                await MarkFailedAsync(db, table, message, ex, cancellationToken);
            }
        }

        return batch.Count;
    }

    /// <summary>
    /// Reclama un lote de mensajes pendientes para su publicación, bloqueándolos temporalmente.
    /// </summary>
    /// <param name="db">El contexto de base de datos.</param>
    /// <param name="table">El nombre de la tabla outbox.</param>
    /// <param name="cancellationToken">El token de cancelación que permite abortar la operación.</param>
    /// <returns>La lista de mensajes reclamados.</returns>
    private static async Task<List<ClaimedMessage>> ClaimAsync(TContext db, string table, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {table}
               SET locked_until = now() + make_interval(secs => @lease)
             WHERE id IN (SELECT id
                            FROM {table}
                           WHERE published_at IS NULL
                             AND (locked_until IS NULL OR locked_until <= now())
                           ORDER BY occurred_at
                           LIMIT @batch
                             FOR UPDATE SKIP LOCKED)
            RETURNING id, event_type, event_version, payload::text, attempts
            """;
        command.Parameters.AddWithValue("lease", (double)LEASE_SECONDS);
        command.Parameters.AddWithValue("batch", BATCH_SIZE);

        var claimed = new List<ClaimedMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            claimed.Add(new ClaimedMessage(
                reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), reader.GetInt32(4)));
        return claimed;
    }

    /// <summary>
    /// Marca un mensaje como fallido, registrando el error y aplicando un backoff antes de reintentar su publicación.
    /// </summary>
    /// <param name="db">El contexto de base de datos.</param>
    /// <param name="table">El nombre de la tabla outbox.</param>
    /// <param name="message">El mensaje que falló.</param>
    /// <param name="error">La excepción que causó el fallo.</param>
    /// <param name="cancellationToken">El token de cancelación que permite abortar la operación.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async Task MarkFailedAsync(
        TContext db, 
        string table, 
        ClaimedMessage message, 
        Exception error, 
        CancellationToken cancellationToken
    )
    {
        logger.LogWarning(error, "No se pudo publicar {EventType} {EventId} (intento {Attempt}). Se reintentará con backoff.",
            message.EventType, message.Id, message.Attempts + 1);

        var text = error.Message.Length > 2000 ? error.Message[..2000] : error.Message;
        try
        {
            // Aplica un backoff exponencial antes de reintentar la publicación del mensaje.
            // Calcula el tiempo de bloqueo basado en un backoff exponencial: 2^intentos segundos, con un máximo de 60 segundos.
            await TrustedSql.ExecuteAsync(
                db,
                $$"""
                  UPDATE {{table}}
                     SET attempts = attempts + 1,
                         last_error = {0},
                         locked_until = now() + (LEAST(power(2, attempts), 60) * interval '1 second')
                   WHERE id = {1}
                  """,
                [text, message.Id], cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Sin esto, el lease vence igual y el evento se reintenta: no hay pérdida, solo menos backoff.
            logger.LogError(ex, "No se pudo registrar el fallo de {EventId}.", message.Id);
        }
    }

    /// <summary>
    /// Construye las propiedades AMQP para un mensaje a partir de la información del mensaje reclamado.
    /// </summary>
    /// <param name="message">El mensaje reclamado del cual se construirán las propiedades AMQP.</param>
    /// <returns>Las propiedades AMQP construidas para el mensaje.</returns>
    private static BasicProperties BuildProperties(ClaimedMessage message) => new()
    {
        MessageId = message.Id.ToString(),
        Type = message.EventType, // sobrevive a los reintentos (la routing key no)
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        CorrelationId = ReadOrderId(message.Payload),
        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        Headers = new Dictionary<string, object?> { [MessageHeaders.EventVersion] = message.EventVersion },
    };

    /// <summary>
    /// Lee el identificador del pedido (orderId) desde el payload JSON del mensaje.
    /// </summary>
    /// <param name="payload">El payload JSON del mensaje.</param>
    /// <returns>El identificador del pedido (orderId) si está presente en el payload; de lo contrario, null.</returns>
    private static string? ReadOrderId(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty("orderId", out var orderId) ? orderId.GetString() : null;
    }
}
