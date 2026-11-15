using Microsoft.EntityFrameworkCore;
using Store.Core.Entities;
using Store.Core.Enums.Orders;
using Store.Infrastructure.Persistence.Common;
using Store.Infrastructure.Persistence.Inventory;

namespace Store.Infrastructure.Persistence.Orders;

/// <summary>
/// Contexto de base de datos para la API de Orders. Maneja el esquema "orders" y las entidades relacionadas con pedidos.
/// Solo se encarga de las entidades relacionadas con pedidos y el esquema "orders".
/// </summary>
/// <param name="options">Las opciones de configuración del DbContext.</param>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Esquema de la base de datos utilizado por este contexto.
    /// </summary>
    public const string SCHEMA = "orders";

    /// <summary>
    /// Conjunto de entidades de pedidos en la base de datos.
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>
    /// Conjunto de mensajes de salida (outbox) en la base de datos.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    /// <summary>
    /// Conjunto de eventos procesados en la base de datos.
    /// </summary>
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    /// <summary>
    /// Conjunto de productos del catálogo en la base de datos.
    /// Solo se utiliza para consultas de lectura del catálogo de productos.
    /// </summary>
    internal DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SCHEMA);

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders", t =>
            {
                t.HasCheckConstraint("ck_orders_quantity",
                    $"quantity BETWEEN {Order.MIN_QUANTITY} AND {Order.MAX_QUANTITY}");
                t.HasCheckConstraint("ck_orders_status",
                    $"status IN ('{EOrderStatus.Pending}', '{EOrderStatus.Confirmed}', '{EOrderStatus.Rejected}')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.HasIndex(x => x.CreatedAt).IsDescending();
        });

        modelBuilder.Entity<CatalogProduct>(b =>
        {
            b.HasNoKey();
            b.ToView("stock", InventorySchema.NAME);
        });

        modelBuilder.AddOutboxAndInbox();
    }
}

/// <summary>
/// Proyección mínima del catálogo que necesita Orders API. Contiene únicamente el SKU del producto.
/// </summary>
internal sealed class CatalogProduct
{
    /// <summary>
    /// Identificador del producto en el catálogo (SKU).
    /// </summary>
    public string Sku { get; private set; } = string.Empty;
}
