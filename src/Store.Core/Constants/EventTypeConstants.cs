namespace Store.Core.Constants;

/// <summary>
/// Constantes que representan los tipos de eventos utilizados en el sistema.
/// </summary>
public static class EventTypeConstants
{
    /// <summary>
    /// Tipo de evento publicado cuando se crea un pedido.
    /// </summary>
    public const string ORDER_CREATED = "orders.created";
    /// <summary>
    /// Tipo de evento publicado cuando se confirma un pedido.
    /// </summary>
    public const string ORDER_CONFIRMED = "orders.stock.confirmed";
    /// <summary>
    /// Tipo de evento publicado cuando se rechaza un pedido.
    /// </summary>
    public const string ORDER_REJECTED = "orders.stock.rejected";
}
