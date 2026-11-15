using RabbitMQ.Client;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Publica mensajes en RabbitMQ utilizando PUBLISHER CONFIRMS: PublishAsync no vuelve hasta que el broker confirma que el mensaje está en una cola (para mensajes persistentes en colas durables, ya en disco). Si el broker lo rechaza (nack) o nadie lo recibe (mandatory + sin binding), lanza una excepción, y el llamador no marca el evento como publicado.
/// </summary>
/// <param name="connection">Conexión a RabbitMQ.</param>
public sealed class RabbitMqPublisher(
    RabbitMqConnection connection
) : IAsyncDisposable
{
    /// <summary>
    /// Opciones para crear un canal con confirmaciones de publicación habilitadas.
    /// </summary>
    private static readonly CreateChannelOptions ConfirmedChannel = new(
        publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChannel? _channel;

    /// <summary>
    /// Publica un mensaje en RabbitMQ utilizando el canal con confirmaciones de publicación.
    /// </summary>
    /// <param name="exchange">Nombre del exchange donde se publicará el mensaje.</param>
    /// <param name="routingKey">Clave de enrutamiento para el mensaje.</param>
    /// <param name="body">Cuerpo del mensaje.</param>
    /// <param name="properties">Propiedades del mensaje.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async Task PublishAsync(
        string exchange, 
        string routingKey, 
        ReadOnlyMemory<byte> body, 
        BasicProperties properties,
        CancellationToken cancellationToken
    )
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Obtiene el canal de RabbitMQ, creando uno nuevo si es necesario.
            var channel = await GetChannelAsync(cancellationToken);
            try
            {
                await channel.BasicPublishAsync(exchange, routingKey, mandatory: true, properties, body, cancellationToken);
            }
            catch
            {
                // Tras un error el canal puede quedar inutilizable: se descarta y se crea otro en la próxima.
                if (!channel.IsOpen) await DiscardChannelAsync();
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Obtiene un canal de RabbitMQ, creando uno nuevo si el actual no está disponible o no está abierto.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El canal de RabbitMQ disponible.</returns>
    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await DiscardChannelAsync();
        var rabbit = await connection.GetAsync(cancellationToken);
        return _channel = await rabbit.CreateChannelAsync(ConfirmedChannel, cancellationToken);
    }

    /// <summary>
    /// Descarta el canal actual de RabbitMQ, liberando los recursos asociados.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async Task DiscardChannelAsync()
    {
        if (_channel is null) return;
        try { await _channel.DisposeAsync(); } catch { /* ya estaba roto */ }
        _channel = null;
    }

    /// <summary>
    /// Libera los recursos asociados al publicador de RabbitMQ, incluyendo el canal y el semáforo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async ValueTask DisposeAsync()
    {
        await DiscardChannelAsync();
        _gate.Dispose();
    }
}
