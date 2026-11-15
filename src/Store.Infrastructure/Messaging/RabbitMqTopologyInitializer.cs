using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Inicializa la topología de RabbitMQ al arrancar el proceso, reintentando hasta que el broker esté disponible.
/// </summary>
/// <param name="connection">Conexión a RabbitMQ; al obtenerla se declara la topología.</param>
/// <param name="logger">Logger del servicio.</param>
public sealed class RabbitMqTopologyInitializer(
    RabbitMqConnection connection,
    ILogger<RabbitMqTopologyInitializer> logger
) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await connection.GetAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ no está disponible; no se pudo declarar la topología. Reintento en {Delay}.", RetryDelay);
                try { await Task.Delay(RetryDelay, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
