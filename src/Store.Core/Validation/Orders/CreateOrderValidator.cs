using Store.Core.Contracts.Catalog;
using Store.Core.Entities;
using Store.Core.Models.Orders;

namespace Store.Core.Validation.Orders;

/// <summary>
/// Valida una solicitud de creación de pedido y devuelve los errores por campo.
/// </summary>
/// <param name="catalog">El catálogo de productos utilizado para validar la existencia del SKU.</param>
public sealed class CreateOrderValidator(IProductCatalog catalog)
{
    public async Task<IReadOnlyDictionary<string, string[]>> ValidateAsync(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            errors["customerName"] = ["El nombre del cliente es obligatorio."];

        if (request.Quantity is < Order.MIN_QUANTITY or > Order.MAX_QUANTITY)
            errors["quantity"] = [$"La cantidad debe estar entre {Order.MIN_QUANTITY} y {Order.MAX_QUANTITY}."];

        if (string.IsNullOrWhiteSpace(request.Sku))
            errors["sku"] = ["El SKU es obligatorio."];
        else if (!await catalog.ExistsAsync(request.Sku.Trim(), cancellationToken))
            errors["sku"] = [$"El SKU '{request.Sku}' no existe en el catálogo."];

        return errors;
    }
}
