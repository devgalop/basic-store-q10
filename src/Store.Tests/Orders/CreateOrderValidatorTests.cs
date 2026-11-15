using Store.Core.Contracts.Catalog;
using Store.Core.Models.Orders;
using Store.Core.Validation.Orders;

namespace Store.Tests.Orders;

/// <summary>
/// Test crítico #2: validación mínima del pedido
/// (clienteNombre no vacío, SKU existente, cantidad entre 1 y 100).
/// </summary>
public class CreateOrderValidatorTests
{
    private sealed class FakeCatalog(params string[] skus) : IProductCatalog
    {
        public Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken)
            => Task.FromResult(skus.Contains(sku));

        public Task<bool> AnyAsync(CancellationToken cancellationToken)
            => Task.FromResult(skus.Length > 0);
    }

    private readonly CreateOrderValidator _validator = new(new FakeCatalog("ABC-01"));

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Valid_request_has_no_errors(int quantity)
    {
        var errors = await _validator.ValidateAsync(new CreateOrderRequest("Ana", "ABC-01", quantity), default);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("", "ABC-01", 1, "customerName")]
    [InlineData("   ", "ABC-01", 1, "customerName")]
    [InlineData("Ana", "NOPE-99", 1, "sku")]
    [InlineData("Ana", "", 1, "sku")]
    [InlineData("Ana", "ABC-01", 0, "quantity")]
    [InlineData("Ana", "ABC-01", 101, "quantity")]
    public async Task Invalid_field_is_reported(string name, string sku, int quantity, string field)
    {
        var errors = await _validator.ValidateAsync(new CreateOrderRequest(name, sku, quantity), default);

        Assert.Single(errors);
        Assert.True(errors.ContainsKey(field));
    }

    [Fact]
    public async Task All_errors_are_reported_together()
    {
        var errors = await _validator.ValidateAsync(new CreateOrderRequest("", "NOPE-99", 0), default);

        Assert.Equal(["customerName", "quantity", "sku"], errors.Keys.Order());
    }
}
