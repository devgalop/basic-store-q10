using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Store.Core.Constants;
using Store.Core.Contracts.Messaging;
using Store.Core.Entities;
using Store.Core.Events;
using Store.Core.UseCases.Inventory;
using Store.Infrastructure.Messaging.Inbox;
using Store.Infrastructure.Persistence;
using Store.Infrastructure.Persistence.Inventory;
using Store.Infrastructure.Serialization;

namespace Store.Tests.Inventory;

/// <summary>
/// PostgreSQL REAL para los tests de inventario. La concurrencia y la atomicidad son propiedades de la
/// base de datos: un mock o SQLite en memoria no demostrarían nada sobre `UPDATE ... WHERE available >= @qty`.
///
/// Toma el servidor y las credenciales de POSTGRES_* (variables de entorno o user-secrets), crea una base
/// temporal exclusiva de esta ejecución, aplica las migraciones reales del Worker y la borra al terminar.
/// No toca la base de la aplicación.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly IConfigurationRoot _serverConfig = new ConfigurationBuilder()
        .AddUserSecrets<PostgresFixture>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    private readonly string _databaseName = $"store_test_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Lanza InvalidOperationException con un mensaje claro si faltan POSTGRES_USER/PASSWORD/DATABASE_NAME.
        var admin = new NpgsqlConnectionStringBuilder(PostgresConnectionString.From(_serverConfig))
        {
            Database = "postgres", // base de mantenimiento: existe siempre
        }.ConnectionString;

        await using (var connection = new NpgsqlConnection(admin))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }

        var testConfig = new ConfigurationBuilder()
            .AddConfiguration(_serverConfig)
            .AddInMemoryCollection(new Dictionary<string, string?> { [DbEnvironmentVariables.POSTGRES_DB] = _databaseName })
            .Build();
        _connectionString = PostgresConnectionString.From(testConfig);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddInventoryPersistence(testConfig);
        // Mismo cableado que usa el Worker en producción (AddInboxConsumer + Handle<OrderCreated, ReserveStockHandler>).
        services.AddScoped<IIntegrationEventHandler<OrderCreated>, ReserveStockHandler>();
        services.AddSingleton<InboxProcessor<InventoryDbContext>>();
        Services = services.BuildServiceProvider();

        await Services.InitializeDatabaseAsync<InventoryDbContext>();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        NpgsqlConnection.ClearAllPools(); // sin esto el DROP falla por conexiones abiertas

        var admin = new NpgsqlConnectionStringBuilder(PostgresConnectionString.From(_serverConfig)) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Procesa un OrderCreated exactamente como lo hace el consumidor: se serializa, se parsea con el mismo
    /// binding de producción y se ejecuta dentro de la transacción del Inbox.
    /// </summary>
    /// <returns>true si se aplicó; false si fue un duplicado.</returns>
    public Task<bool> ProcessAsync(OrderCreated @event)
    {
        var envelope = new EventBinding<OrderCreated>().Parse(IntegrationEventSerializer.Serialize(@event));
        return Services.GetRequiredService<InboxProcessor<InventoryDbContext>>().ProcessAsync(envelope, CancellationToken.None);
    }

    public static OrderCreated NewOrder(string sku, int quantity) =>
        new(Guid.NewGuid(), Guid.NewGuid(), sku, quantity, DateTime.UtcNow);

    /// <summary>Crea un SKU exclusivo del test, así los tests no se pisan entre sí.</summary>
    public async Task<string> AddStockAsync(int available)
    {
        var sku = $"T-{Guid.NewGuid():N}"[..20];
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Stock.Add(new StockItem(sku, "Producto de prueba", available));
        await db.SaveChangesAsync();
        return sku;
    }

    public Task<int> AvailableAsync(string sku) =>
        ScalarAsync<int>("SELECT available FROM inventory.stock WHERE sku = $1", sku);

    /// <summary>Cuántos eventos de resultado hay en el outbox del Worker para ese SKU.</summary>
    public Task<int> OutboxCountAsync(string eventType, string sku) =>
        ScalarAsync<int>(
            "SELECT count(*)::int FROM inventory.outbox_messages WHERE event_type = $1 AND payload->>'sku' = $2",
            eventType, sku);

    public Task<int> InboxCountAsync(Guid eventId) =>
        ScalarAsync<int>("SELECT count(*)::int FROM inventory.processed_events WHERE event_id = $1", eventId);

    public Task<string> RejectionReasonAsync(string sku) =>
        ScalarAsync<string>(
            "SELECT payload->>'reason' FROM inventory.outbox_messages WHERE event_type = $1 AND payload->>'sku' = $2",
            StockRejected.EventType, sku);

    private async Task<T> ScalarAsync<T>(string sql, params object[] args)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var arg in args) command.Parameters.Add(new NpgsqlParameter { Value = arg });
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
