namespace Store.Core.Entities;

/// <summary>
/// Representa un ítem de stock disponible de un producto.
/// </summary>
public sealed class StockItem
{
    /// <summary>
    /// Identificador del producto (SKU).
    /// </summary>
    public string Sku { get; private set; } = string.Empty;
    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Cantidad disponible en stock.
    /// </summary>
    public int Available { get; private set; }

    private StockItem() { }

    /// <summary>
    /// Crea una nueva instancia de StockItem.
    /// </summary>
    /// <param name="sku">Identificador del producto (SKU).</param>
    /// <param name="name">Nombre del producto.</param>
    /// <param name="available">Cantidad disponible en stock.</param>
    /// <exception cref="ArgumentException">Se lanza si el SKU es nulo, vacío o contiene solo espacios en blanco.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la cantidad disponible es negativa.</exception>
    public StockItem(string sku, string name, int available)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        ArgumentOutOfRangeException.ThrowIfNegative(available);

        Sku = sku;
        Name = name;
        Available = available;
    }
}
