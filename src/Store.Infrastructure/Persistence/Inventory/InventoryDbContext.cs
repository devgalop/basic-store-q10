using Microsoft.EntityFrameworkCore;
using Store.Core.Entities;
using Store.Infrastructure.Persistence.Common;

namespace Store.Infrastructure.Persistence.Inventory;

/// <summary>
/// Contexto de base de datos para la persistencia de inventario. Maneja el esquema "inventory" y es responsable del stock.
/// El único responsable de gestionar el stock disponible en el inventario.
/// </summary>
/// <param name="options">Las opciones de configuración del DbContext.</param>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> Stock => Set<StockItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(InventorySchema.NAME);

        modelBuilder.Entity<StockItem>(b =>
        {
            b.ToTable("stock", t => t.HasCheckConstraint("ck_stock_available_non_negative", "available >= 0"));
            b.HasKey(x => x.Sku);
            b.Property(x => x.Sku).HasMaxLength(50);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.AddOutboxAndInbox();
    }
}
