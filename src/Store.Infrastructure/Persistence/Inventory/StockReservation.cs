using Microsoft.EntityFrameworkCore;
using Store.Core.Contracts.Inventory;

namespace Store.Infrastructure.Persistence.Inventory;

/// <summary>
/// Implementación de IStockReservation que intenta reservar stock en la base de datos de manera atómica.
/// </summary>
/// <param name="db">Contexto de base de datos para acceder a la tabla de stock.</param>
internal sealed class StockReservation(InventoryDbContext db) : IStockReservation
{
    /// <summary>
    /// Intenta reservar la cantidad especificada de stock para el SKU dado de manera atómica.
    /// </summary>
    /// <param name="sku">SKU del producto cuyo stock se desea reservar.</param>
    /// <param name="quantity">Cantidad de stock a reservar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>True si se pudo reservar el stock; false si no hay suficiente stock disponible.</returns>
    public async Task<bool> TryReserveAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        var affected = await db.Stock
            .Where(s => s.Sku == sku && s.Available >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Available, x => x.Available - quantity), cancellationToken);

        // Si affected es 1, significa que se pudo descontar el stock; si es 0, no hay suficiente stock disponible.
        return affected == 1;
    }
}
