using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Store.Infrastructure.Messaging.Inbox;

/// <summary>
/// Opciones de configuración para el consumidor de la bandeja de entrada (Inbox).
/// </summary>
public sealed class InboxConsumerOptions
{
    /// <summary>
    /// Nombre de la cola de la bandeja de entrada (Inbox) que se va a consumir.
    /// </summary>
    public required string Queue { get; init; }

    /// <summary>
    /// Número máximo de mensajes que el broker puede entregar sin confirmar al consumidor.
    /// Por defecto se configuran 10 mensajes.
    /// </summary>
    public ushort Prefetch { get; init; } = 10;

    /// <summary>
    /// Número máximo de mensajes que se pueden procesar en paralelo dentro del proceso. Cada uno usa su propio scope y DbContext.
    /// Por defecto se configuran 4 mensajes.
    /// </summary>
    public ushort Concurrency { get; init; } = 4;

    /// <summary>
    /// Número máximo de intentos ante fallos técnicos antes de mandar el mensaje a la DLQ.
    /// Por defecto se configuran 5 intentos.
    /// </summary>
    public int MaxAttempts { get; init; } = 5;
}

/// <summary>
/// Consumidor idempotente. Por cada mensaje, en UNA transacción de PostgreSQL:
///   1. INSERT en processed_events ... ON CONFLICT DO NOTHING  (Inbox)
///   2. el handler de negocio (mismo DbContext = misma transacción)
///   3. COMMIT, y solo entonces ACK al broker.
///
/// - Duplicado (el INSERT no insertó nada): no se ejecuta el handler; se hace ACK.
///   Si dos instancias reciben el mismo evento a la vez, la segunda espera la PK de la primera.
/// - Muere entre el COMMIT y el ACK: el broker re-entrega, el Inbox lo detecta y se hace ACK.
/// - Fallo técnico (handler lanza): rollback completo + nack sin requeue → cola .retry → vuelve tras
///   RetryDelay. Al agotar MaxAttempts, el mensaje va a la DLQ. NUNCA se convierte en rechazo de negocio.
/// - Mensaje veneno (tipo/versión desconocidos, JSON inválido): reintentar no lo arregla → DLQ directo.
/// </summary>
public sealed class InboxConsumer<TContext> : BackgroundService where TContext : DbContext
{
    private readonly TimeSpan SetupRetryDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(5);

    private readonly InboxConsumerOptions _options;
    private readonly IReadOnlyDictionary<string, IEventBinding> _bindings;
    private readonly RabbitMqConnection _rabbit;
    private readonly RabbitMqPublisher _publisher;
    private readonly InboxProcessor<TContext> _processor;
    private readonly TimeProvider _clock;
    private readonly ILogger<InboxConsumer<TContext>> _logger;

    internal InboxConsumer(
        InboxConsumerOptions options,
        IReadOnlyDictionary<string, IEventBinding> bindings,
        RabbitMqConnection rabbit,
        RabbitMqPublisher publisher,
        InboxProcessor<TContext> processor,
        TimeProvider clock,
        ILogger<InboxConsumer<TContext>> logger)
    {
        _options = options;
        _bindings = bindings;
        _rabbit = rabbit;
        _publisher = publisher;
        _processor = processor;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Excepción que indica que el mensaje recibido es un mensaje veneno (poison message).
    /// Se lanza cuando se detecta que un mensaje no puede ser procesado correctamente debido a que es un mensaje veneno.
    /// </summary>
    /// <param name="message">Mensaje de error que describe el motivo del mensaje veneno.</param>
    /// <param name="inner">Excepción interna que causó el mensaje veneno, si aplica.</param>
    public sealed class PoisonMessageException(string message, Exception? inner = null)
        : Exception(message, inner);

    /// <summary>
    /// Método principal que ejecuta el consumo de mensajes desde la cola.
    /// </summary>
    /// <param name="stoppingToken">Token de cancelación para detener el consumo de mensajes.</param>
    /// <returns>Tarea que representa la operación asincrónica de consumo de mensajes.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Broker caído al arrancar (o canal roto): se reintenta hasta que vuelva.
                _logger.LogWarning(ex, "No se pudo consumir {Queue}. Reintento en {Delay}.", _options.Queue, SetupRetryDelay);
                try { await Task.Delay(SetupRetryDelay, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    /// <summary>
    /// Consume los mensajes de la cola, creando un canal y un consumidor.
    /// Lanza InvalidOperationException si el canal se cierra inesperadamente.
    /// </summary>
    /// <param name="stoppingToken">Token de cancelación para detener el consumo de mensajes.</param>
    /// <returns>Tarea que representa la operación asincrónica de consumo de mensajes.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el canal de la cola se cierra inesperadamente.</exception>
    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var connection = await _rabbit.GetAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false,
                consumerDispatchConcurrency: _options.Concurrency),
            stoppingToken);

        // Configura la calidad de servicio del canal (prefetch) para controlar la cantidad de mensajes no reconocidos que se pueden entregar al consumidor.
        await channel.BasicQosAsync(0, _options.Prefetch, global: false, stoppingToken);

        // Crea un consumidor asincrónico para procesar los mensajes recibidos.
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnDeliveryAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(_options.Queue, autoAck: false, consumer, stoppingToken);

        _logger.LogInformation("Consumiendo {Queue} (prefetch {Prefetch}, concurrencia {Concurrency}).",
            _options.Queue, _options.Prefetch, _options.Concurrency);

        // Una caída de conexión la recupera el cliente solo. Un error a nivel de canal (conexión viva,canal cerrado) no se recupera: se detecta acá y se recrea el consumidor.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(WatchdogInterval, stoppingToken);
            if (connection.IsOpen && channel.IsClosed)
                throw new InvalidOperationException($"El canal de {_options.Queue} se cerró; se recreará el consumidor.");
        }
    }

    /// <summary>
    /// Maneja la entrega de un mensaje recibido por el consumidor, procesándolo y reconociéndolo o manejando errores según corresponda.
    /// </summary>
    /// <param name="channel">Canal de RabbitMQ por el cual se recibió el mensaje.</param>
    /// <param name="delivery">Evento de entrega que contiene el mensaje y sus propiedades.</param>
    /// <param name="stoppingToken">Token de cancelación para detener el procesamiento del mensaje.</param>
    /// <returns>Tarea que representa la operación asincrónica de manejo del mensaje.</returns>
    private async Task OnDeliveryAsync(
        IChannel channel, 
        BasicDeliverEventArgs delivery, 
        CancellationToken stoppingToken
    )
    {
        try
        {
            await HandleDeliveryAsync(channel, delivery, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado: sin ack, el broker re-entrega el mensaje a la próxima instancia.
        }
        catch (Exception ex)
        {
            // Ej.: el canal se cerró antes del ack. El broker re-entrega y el Inbox absorbe el duplicado.
            _logger.LogError(ex, "Error al resolver el mensaje {MessageId} de {Queue}.",
                delivery.BasicProperties.MessageId, _options.Queue);
        }
    }

    /// <summary>
    /// Maneja la entrega de un mensaje, procesándolo dentro de una transacción y reconociéndolo o enviándolo a la cola de mensajes fallidos según corresponda.
    /// </summary>
    /// <param name="channel">Canal de RabbitMQ por el cual se recibió el mensaje.</param>
    /// <param name="delivery">Evento de entrega que contiene el mensaje y sus propiedades.</param>
    /// <param name="stoppingToken">Token de cancelación para detener el procesamiento del mensaje.</param>
    /// <returns>Tarea que representa la operación asincrónica de manejo del mensaje.</returns>
    private async Task HandleDeliveryAsync(
        IChannel channel, 
        BasicDeliverEventArgs delivery, 
        CancellationToken stoppingToken
    )
    {
        var properties = delivery.BasicProperties;
        var body = delivery.Body.ToArray();

        EventEnvelope envelope;
        try
        {
            envelope = Parse(properties, body);
        }
        catch (PoisonMessageException ex)
        {
            await DeadLetterAsync(channel, delivery, body, ex.Message, FailedAttempts(properties) + 1, stoppingToken);
            return;
        }

        try
        {
            var applied = await _processor.ProcessAsync(envelope, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);

            if (applied)
                _logger.LogInformation("Evento {EventType} {EventId} procesado.", envelope.EventType, envelope.EventId);
            else
                _logger.LogInformation("Evento {EventType} {EventId} ya procesado antes: duplicado ignorado.",
                    envelope.EventType, envelope.EventId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var attempt = FailedAttempts(properties) + 1;
            if (attempt >= _options.MaxAttempts)
            {
                _logger.LogError(ex, "Evento {EventType} {EventId} agotó {Max} intentos.",
                    envelope.EventType, envelope.EventId, _options.MaxAttempts);
                await DeadLetterAsync(channel, delivery, body, ex.Message, attempt, stoppingToken);
            }
            else
            {
                _logger.LogWarning(ex, "Falló el evento {EventType} {EventId} (intento {Attempt}/{Max}). Pasa a reintento.",
                    envelope.EventType, envelope.EventId, attempt, _options.MaxAttempts);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Parsea el mensaje recibido desde RabbitMQ a un EventEnvelope.
    /// </summary>
    /// <param name="properties">Propiedades del mensaje recibido.</param>
    /// <param name="body">Cuerpo del mensaje recibido.</param>
    /// <returns>EventEnvelope que representa el evento parseado.</returns>
    /// <exception cref="PoisonMessageException">Se lanza cuando el mensaje no puede ser parseado correctamente, ya sea por un tipo desconocido, una versión no soportada o un payload inválido.</exception>
    private EventEnvelope Parse(
        IReadOnlyBasicProperties properties, 
        byte[] body
    )
    {
        if (properties.Type is null || !_bindings.TryGetValue(properties.Type, out var binding))
            throw new PoisonMessageException($"Tipo de evento desconocido para esta cola: '{properties.Type}'.");

        // Una versión mayor a la que este servicio entiende no se puede interpretar de forma segura.
        if (properties.Headers?.TryGetValue(MessageHeaders.EventVersion, out var raw) == true
            && raw is not null && Convert.ToInt32(raw) > binding.Version)
            throw new PoisonMessageException(
                $"Versión {raw} de '{properties.Type}' no soportada (máxima conocida: {binding.Version}).");

        try
        {
            return binding.Parse(Encoding.UTF8.GetString(body));
        }
        catch (JsonException ex)
        {
            throw new PoisonMessageException($"Payload inválido para '{properties.Type}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Envía el mensaje a la Dead Letter Queue (DLQ) en caso de fallo.
    /// </summary>
    /// <param name="channel">El canal de RabbitMQ.</param>
    /// <param name="delivery">Los argumentos de entrega del mensaje.</param>
    /// <param name="body">El cuerpo del mensaje.</param>
    /// <param name="reason">La razón por la cual se envía a la DLQ.</param>
    /// <param name="attempts">El número de intentos fallidos.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async Task DeadLetterAsync(
        IChannel channel, 
        BasicDeliverEventArgs delivery, 
        byte[] body, 
        string reason, 
        int attempts, 
        CancellationToken ct
    )
    {
        try
        {
            var original = delivery.BasicProperties;
            var properties = new BasicProperties
            {
                MessageId = original.MessageId,
                Type = original.Type,
                ContentType = original.ContentType,
                CorrelationId = original.CorrelationId,
                DeliveryMode = DeliveryModes.Persistent,
                Headers = new Dictionary<string, object?>
                {
                    [MessageHeaders.EventVersion] =
                        original.Headers is not null && original.Headers.TryGetValue(MessageHeaders.EventVersion, out var version)
                            ? version
                            : null,
                    [MessageHeaders.Error] = reason.Length > 1000 ? reason[..1000] : reason,
                    [MessageHeaders.FailedAttempts] = attempts,
                    [MessageHeaders.OriginalQueue] = _options.Queue,
                    [MessageHeaders.FailedAt] = _clock.GetUtcNow().ToString("O"),
                },
            };

            // Con confirm: el mensaje solo se ackea de la cola original cuando la DLQ ya lo tiene.
            await _publisher.PublishAsync(RabbitMqTopology.DeadLetterExchange, _options.Queue, body, properties, ct);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);

            _logger.LogError("Mensaje {MessageId} enviado a {Dlq}: {Reason}",
                original.MessageId, RabbitMqTopology.DeadLetterQueueOf(_options.Queue), reason);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Si la DLQ no lo aceptó, no se pierde: vuelve a la cola de reintento y se intenta de nuevo.
            _logger.LogError(ex, "No se pudo enviar el mensaje {MessageId} a la DLQ; vuelve a reintento.",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
        }
    }

    /// <summary>
    /// Obtiene el número de intentos fallidos de un mensaje en la cola actual.
    /// </summary>
    /// <param name="properties">Las propiedades del mensaje que contienen los headers de RabbitMQ.</param>
    /// <returns>El número de intentos fallidos.</returns>
    private int FailedAttempts(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue(MessageHeaders.Death, out var raw) != true || raw is not IList<object> deaths)
            return 0;

        long total = 0;
        foreach (var death in deaths.OfType<IDictionary<string, object>>())
        {
            if (AsText(death, "queue") == _options.Queue && AsText(death, "reason") == "rejected")
                total += Convert.ToInt64(death["count"]);
        }
        return (int)total;
    }

    /// <summary>
    /// Convierte el valor de una tabla AMQP a texto, si es un byte[] UTF-8 o un string.
    /// </summary>
    /// <param name="table">La tabla AMQP que contiene el valor.</param>
    /// <param name="key">La clave del valor a obtener.</param>
    /// <returns>El valor como texto, o null si no se puede convertir.</returns>
    private static string? AsText(IDictionary<string, object> table, string key) =>
        table.TryGetValue(key, out var value)
            ? value switch { byte[] bytes => Encoding.UTF8.GetString(bytes), string text => text, _ => null }
            : null;
}
