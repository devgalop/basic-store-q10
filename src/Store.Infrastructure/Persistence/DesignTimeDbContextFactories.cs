using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Store.Infrastructure.Persistence.Inventory;
using Store.Infrastructure.Persistence.Orders;

namespace Store.Infrastructure.Persistence;

/// <summary>
/// Proporciona la cadena de conexión para uso en tiempo de diseño, como al agregar migraciones con `dotnet ef migrations add`.
/// </summary>
internal static class DesignTime
{
    public const string ConnectionString = "Host=localhost;Database=design-time-only";
}

internal sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>();
        PersistenceServiceCollectionExtensions.Configure(options, DesignTime.ConnectionString, OrdersDbContext.SCHEMA);
        return new OrdersDbContext(options.Options);
    }
}

internal sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        PersistenceServiceCollectionExtensions.Configure(options, DesignTime.ConnectionString, InventorySchema.NAME);
        options.UseInventorySeed();
        return new InventoryDbContext(options.Options);
    }
}
