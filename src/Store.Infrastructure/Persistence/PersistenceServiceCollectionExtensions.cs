using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.Core.Contracts.Catalog;
using Store.Core.Contracts.Inventory;
using Store.Core.Contracts.Messaging;
using Store.Core.Contracts.Orders;
using Store.Infrastructure.Persistence.Common;
using Store.Infrastructure.Persistence.Inventory;
using Store.Infrastructure.Persistence.Orders;

namespace Store.Infrastructure.Persistence;

/// <summary>
/// Extensiones para registrar la persistencia de datos en el contenedor de servicios.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registra la persistencia de órdenes en el contenedor de servicios.
    /// </summary>
    /// <param name="services">El contenedor de servicios donde se registrará la persistencia.</param>
    /// <param name="configuration">La configuración de la aplicación que contiene la cadena de conexión.</param>
    /// <returns>El contenedor de servicios con la persistencia de órdenes registrada.</returns>
    public static IServiceCollection AddOrdersPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PostgresConnectionString.From(configuration);
        services.AddDbContext<OrdersDbContext>(o => Configure(o, connectionString, OrdersDbContext.SCHEMA));
        services.AddScoped<IProductCatalog, ProductCatalog>();
        services.AddScoped<IOrderStore, OrderStore>();
        return services;
    }

    /// <summary>
    /// Registra la persistencia de inventario en el contenedor de servicios.
    /// </summary>
    /// <param name="services">El contenedor de servicios donde se registrará la persistencia.</param>
    /// <param name="configuration">La configuración de la aplicación que contiene la cadena de conexión.</param>
    /// <returns>El contenedor de servicios con la persistencia de inventario registrada.</returns>
    public static IServiceCollection AddInventoryPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PostgresConnectionString.From(configuration);
        services.AddDbContext<InventoryDbContext>(o =>
        {
            Configure(o, connectionString, InventorySchema.NAME);
            o.UseInventorySeed();
        });
        services.AddScoped<IStockReservation, StockReservation>();
        services.AddScoped<IEventOutbox>(sp => new DbContextEventOutbox(sp.GetRequiredService<InventoryDbContext>()));
        return services;
    }

    /// <summary>
    /// Configura las opciones de DbContext con la cadena de conexión y el esquema especificados.
    /// </summary>
    /// <param name="options">Las opciones de DbContext a configurar.</param>
    /// <param name="connectionString">La cadena de conexión a la base de datos.</param>
    /// <param name="schema">El esquema de la base de datos donde se almacenarán las migraciones.</param>
    internal static void Configure(DbContextOptionsBuilder options, string connectionString, string schema) =>
        options
        .UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", schema);
        })
        .UseSnakeCaseNamingConvention();
}
