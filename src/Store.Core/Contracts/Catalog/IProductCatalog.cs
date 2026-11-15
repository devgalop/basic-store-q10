namespace Store.Core.Contracts.Catalog;

/// <summary>
/// Consulta de solo lectura sobre el catálogo de productos.
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Verifica si un producto con el SKU especificado existe en el catálogo.
    /// </summary>
    /// <param name="sku">El SKU del producto a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si el producto existe en el catálogo; de lo contrario, false.</returns>
    Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken);

    /// <summary>
    /// Indica si el catálogo tiene al menos un producto. Sirve para saber si el Inventory Worker ya terminó de
    /// crear su schema Y de cargar el seed: con la tabla creada pero vacía, todo SKU "no existe".
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>True si hay al menos un producto; de lo contrario, false.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken);
}