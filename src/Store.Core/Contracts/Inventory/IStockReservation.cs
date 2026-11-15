namespace Store.Core.Contracts.Inventory;

/// <summary>
/// Reserva de stock. PostgreSQL es la fuente de verdad: la implementación NO puede basarse en
/// leer el stock y compararlo en memoria (dos workers leerían el mismo valor y venderían de más).
/// </summary>
public interface IStockReservation
{
    /// <summary>
    /// Descuenta <paramref name="quantity"/> unidades del SKU de forma atómica, solo si hay suficientes.
    /// </summary>
    /// <param name="sku">Código del producto.</param>
    /// <param name="quantity">Cantidad a reservar (mayor a cero).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>
    /// true si se descontó; false si no hay stock suficiente (rechazo de negocio, no un error).
    /// Un fallo de infraestructura NO devuelve false: lanza excepción, para que el mensaje se reintente.
    /// </returns>
    Task<bool> TryReserveAsync(string sku, int quantity, CancellationToken cancellationToken);
}
