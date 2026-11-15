using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Store.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Inicializa la base de datos para el contexto especificado. Crea la base si no existe y aplica las migraciones pendientes.
    /// </summary>
    /// <typeparam name="TContext">El tipo de contexto de base de datos a inicializar.</typeparam>
    /// <param name="services">El proveedor de servicios que contiene el contexto de base de datos.</param>
    /// <param name="cancellationToken">El token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asincrónica de inicialización de la base de datos.</returns>
    public static async Task InitializeDatabaseAsync<TContext>(
        this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        const int MAX_ATTEMPTS = 10;
        var contextName = typeof(TContext).Name;
        var logger = services.GetRequiredService<ILoggerFactory>()
                            .CreateLogger(nameof(DatabaseInitializer));

        for (var attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();

                var creator = db.GetService<IRelationalDatabaseCreator>();
                if (!await creator.ExistsAsync(cancellationToken))
                {
                    logger.LogInformation("La base de datos no existe. Creándola ({Context}).", contextName);
                    await creator.CreateAsync(cancellationToken);
                }

                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                logger.LogInformation("{Context}: {Count} migraciones pendientes {Migrations}.",
                    contextName, pending.Count, pending);

                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Base de datos lista para {Context}.", contextName);
                return;
            }
            catch (Exception ex) when (attempt < MAX_ATTEMPTS && ex is not OperationCanceledException)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(2 * attempt, 10));
                logger.LogWarning(ex, "No se pudo inicializar {Context} (intento {Attempt}/{Max}). Reintento en {Delay}.",
                    contextName, attempt, MAX_ATTEMPTS, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
