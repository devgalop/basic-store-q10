using Microsoft.EntityFrameworkCore;
using Store.Core.Contracts.Catalog;

namespace Store.Infrastructure.Persistence.Orders;

/// <summary>
/// Implementación de IProductCatalog que utiliza OrdersDbContext para verificar la existencia de productos en el catálogo.
/// </summary>
/// <param name="db">El contexto de base de datos utilizado para verificar la existencia de productos en el catálogo.</param>
internal sealed class ProductCatalog(OrdersDbContext db) : IProductCatalog
{
    /// <summary>
    /// Verifica si un producto con el SKU especificado existe en el catálogo.
    /// </summary>
    /// <param name="sku">El SKU del producto a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>True si el producto existe en el catálogo, de lo contrario false.</returns>
    public Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken) =>
        db.CatalogProducts.AnyAsync(p => p.Sku == sku, cancellationToken);

    /// <summary>
    /// Indica si el catálogo tiene al menos un producto.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>True si hay al menos un producto, de lo contrario false.</returns>
    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        db.CatalogProducts.AnyAsync(cancellationToken);
}
