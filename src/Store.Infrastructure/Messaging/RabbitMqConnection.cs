using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Representa una conexión a RabbitMQ que se encarga de crear y declarar la topología si es necesario.
/// </summary>
/// <param name="settings">Configuración de RabbitMQ.</param>
/// <param name="logger">Instancia de logger para registrar eventos relacionados con la conexión.</param>
public sealed class RabbitMqConnection(
    RabbitMqSettings settings, 
    ILogger<RabbitMqConnection> logger
) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    /// <summary>
    /// Obtiene la conexión a RabbitMQ, creando y declarando la topología si es necesario.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>La conexión a RabbitMQ establecida.</returns>
    /// <exception cref="OperationCanceledException">Si la operación es cancelada mediante el token de cancelación.</exception>
    /// <exception cref="Exception">Si ocurre un error al establecer la conexión o declarar la topología.</exception>
    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        // Si ya existe, se devuelve aunque esté recuperándose: crear otra duplicaría consumidores.
        if (_connection is not null) return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null) return _connection;

            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.User,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
            };

            var connection = await factory.CreateConnectionAsync("store", cancellationToken);
            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await RabbitMqTopology.DeclareAsync(channel, settings, cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }

            logger.LogInformation("Conectado a RabbitMQ en {Host}:{Port} y topología declarada.", settings.Host, settings.Port);
            return _connection = connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Libera los recursos asociados a la conexión a RabbitMQ y al semáforo de exclusión mutua.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona de liberación de recursos.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
