namespace Store.Core.Enums.Stock;

/// <summary>
/// Motivo estructurado de rechazo de una reserva. Se serializa como string.
/// </summary>
public enum EStockRejectionReason
{
    /// <summary>
    /// El stock disponible es menor a la cantidad solicitada.
    /// </summary>
    InsufficientStock
}
