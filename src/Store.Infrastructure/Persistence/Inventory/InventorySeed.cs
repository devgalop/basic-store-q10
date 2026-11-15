using Microsoft.EntityFrameworkCore;
using Store.Core.Entities;

namespace Store.Infrastructure.Persistence.Inventory;

/// <summary>
/// Proporciona los datos iniciales de inventario para la base de datos.
/// </summary>
internal static class InventorySeed
{
    /// <summary>
    /// Obtiene los productos iniciales de inventario.
    /// </summary>
    /// <returns>Un arreglo de elementos de stock iniciales.</returns>
    private static StockItem[] Products() =>
    [
        new("ABC-01", "Teclado mecánico", 100),
        new("XYZ-02", "Mouse inalámbrico", 50),
        new("LMN-03", "Monitor 27\"", 5),
        new("OUT-04", "Webcam HD", 0),
    ];

    /// <summary>
    /// Aplica la siembra de inventario al DbContextOptionsBuilder.
    /// Inserta los productos iniciales en la base de datos si la tabla de stock está vacía.
    /// </summary>
    /// <param name="options">El DbContextOptionsBuilder a configurar con la siembra de inventario.</param>
    /// <returns>El DbContextOptionsBuilder configurado con la siembra de inventario.</returns>
    public static DbContextOptionsBuilder UseInventorySeed(this DbContextOptionsBuilder options) =>
        options
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                var stock = context.Set<StockItem>();
                if (await stock.AnyAsync(cancellationToken)) return;

                stock.AddRange(Products());
                await context.SaveChangesAsync(cancellationToken);
            })
            .UseSeeding((context, _) =>
            {
                var stock = context.Set<StockItem>();
                if (stock.Any()) return;

                stock.AddRange(Products());
                context.SaveChanges();
            });
}
