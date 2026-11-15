using Store.Core.Entities;

namespace Store.Core.Models.Orders;

/// <summary>
/// Resultado de crear un pedido: o el pedido creado, o los errores de validación por campo.
/// Los datos inválidos son un resultado esperado, no una excepción.
/// </summary>
/// <param name="Order">El pedido creado, si la creación fue exitosa.</param>
/// <param name="Errors">Los errores de validación por campo, si la creación fue inválida.</param>
public sealed record CreateOrderResult(
    Order? Order, 
    IReadOnlyDictionary<string, string[]> Errors
)
{
    /// <summary>
    /// Indica si la creación del pedido fue válida.
    /// </summary>
    public bool IsValid => Order is not null;

    /// <summary>
    /// Crea un resultado de pedido exitoso con el pedido proporcionado.
    /// </summary>
    /// <param name="order">El pedido creado.</param>
    /// <returns>Un resultado de creación de pedido exitoso.</returns>
    public static CreateOrderResult Created(Order order) =>
        new(order, new Dictionary<string, string[]>());

    /// <summary>
    /// Crea un resultado de pedido inválido con los errores de validación proporcionados.
    /// </summary>
    /// <param name="errors">Los errores de validación por campo.</param>
    /// <returns>Un resultado de creación de pedido inválido.</returns>
    public static CreateOrderResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}
