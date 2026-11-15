using RabbitMQ.Client;
using Store.Core.Events;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Define la topología de RabbitMQ utilizada por el sistema, incluyendo exchanges, colas y bindings.
/// </summary>
public static class RabbitMqTopology
{
    public const string Exchange = "orders";
    public const string RetryExchange = "orders.retry";
    public const string DeadLetterExchange = "orders.dlx";

    public const string OrderCreatedQueue = "inventory.order-created";
    public const string StockResultsQueue = "orders.stock-results";

    private sealed record QueueDefinition(string Name, string[] RoutingKeys);

    /// <summary>
    /// Definiciones de colas utilizadas en la topología de RabbitMQ, incluyendo sus routing keys asociadas.
    /// </summary>
    private static readonly QueueDefinition[] Queues =
    [
        new(OrderCreatedQueue, [OrderCreated.EventType]),
        new(StockResultsQueue, [StockReserved.EventType, StockRejected.EventType]),
    ];

    /// <summary>
    /// Obtiene el nombre de la cola de reintento asociada a la cola principal especificada.
    /// </summary>
    /// <param name="queue">Nombre de la cola principal.</param>
    /// <returns>Nombre de la cola de reintento correspondiente.</returns>
    public static string RetryQueueOf(string queue) => $"{queue}.retry";
    /// <summary>
    /// Obtiene el nombre de la cola de dead-letter asociada a la cola principal especificada.
    /// </summary>
    /// <param name="queue">Nombre de la cola principal.</param>
    /// <returns>Nombre de la cola de dead-letter correspondiente.</returns>
    public static string DeadLetterQueueOf(string queue) => $"{queue}.dlq";

    /// <summary>
    /// Declara los exchanges, colas y bindings de RabbitMQ según la topología definida.
    /// </summary>
    /// <param name="channel">Canal de RabbitMQ donde se declararán los elementos.</param>
    /// <param name="settings">Configuración de RabbitMQ.</param>
    /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
    /// <returns>Tarea que representa la operación asíncrona de declaración.</returns>
    public static async Task DeclareAsync(
        IChannel channel, 
        RabbitMqSettings settings, 
        CancellationToken cancellationToken
    )
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        foreach (var queue in Queues)
        {
            // Cola principal: si el consumidor hace nack(requeue:false), el mensaje pasa a la cola de reintento.
            await DeclareQueueAsync(channel, queue.Name, new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RetryExchange,
                ["x-dead-letter-routing-key"] = queue.Name,
            }, cancellationToken);

            // Cola de reintento: espera RetryDelay (TTL por mensaje) y al expirar lo devuelve, vía el
            // exchange por defecto, a la cola principal. Es un delay sin bloquear ningún consumidor.
            await DeclareQueueAsync(channel, RetryQueueOf(queue.Name), new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (int)settings.RetryDelay.TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = queue.Name,
            }, cancellationToken);

            // DLQ: sin consumidor automático. Los mensajes se inspeccionan y se reinyectan a mano.
            await DeclareQueueAsync(channel, DeadLetterQueueOf(queue.Name), arguments: null, cancellationToken);

            foreach (var key in queue.RoutingKeys)
                await channel.QueueBindAsync(queue.Name, Exchange, key, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(RetryQueueOf(queue.Name), RetryExchange, queue.Name, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(DeadLetterQueueOf(queue.Name), DeadLetterExchange, queue.Name, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Crea una cola en RabbitMQ con los parámetros especificados.
    /// </summary>
    /// <param name="channel">Canal de RabbitMQ donde se declarará la cola.</param>
    /// <param name="name">Nombre de la cola a crear.</param>
    /// <param name="arguments">Argumentos adicionales para la cola (por ejemplo, dead-letter exchange, TTL).</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>Tarea que representa la operación asíncrona de declaración de la cola.</returns>
    private static Task DeclareQueueAsync(
        IChannel channel, string name, Dictionary<string, object?>? arguments, CancellationToken cancellationToken) =>
        channel.QueueDeclareAsync(name, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: cancellationToken);
}
