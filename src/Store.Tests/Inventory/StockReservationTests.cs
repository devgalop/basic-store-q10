using Store.Core.Events;
using static Store.Tests.Inventory.PostgresFixture;

namespace Store.Tests.Inventory;

/// <summary>
/// Pruebas de la lógica crítica del Worker contra PostgreSQL real: idempotencia, rechazo sin efectos
/// parciales y NO sobreventa bajo concurrencia. Cada test usa su propio SKU.
/// </summary>
public class StockReservationTests(PostgresFixture db) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Duplicate_delivery_decrements_stock_only_once()
    {
        var sku = await db.AddStockAsync(10);
        var order = NewOrder(sku, quantity: 3);

        Assert.True(await db.ProcessAsync(order));   // primera entrega: se aplica
        Assert.False(await db.ProcessAsync(order));  // misma entrega otra vez: se ignora

        // 10 re-entregas simultáneas (varias instancias recibiendo el duplicado a la vez).
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => db.ProcessAsync(order))));

        Assert.All(results, applied => Assert.False(applied));
        Assert.Equal(7, await db.AvailableAsync(sku));                                   // 10 - 3, no 10 - 3*12
        Assert.Equal(1, await db.InboxCountAsync(order.EventId));
        Assert.Equal(1, await db.OutboxCountAsync(StockReserved.EventType, sku));        // un solo resultado publicado
    }

    [Fact]
    public async Task Insufficient_stock_is_rejected_without_touching_the_stock()
    {
        var sku = await db.AddStockAsync(3);
        var order = NewOrder(sku, quantity: 5);

        // Un rechazo de negocio NO es un error: el evento se procesa y se registra como procesado.
        Assert.True(await db.ProcessAsync(order));

        Assert.Equal(3, await db.AvailableAsync(sku));                                   // ni una unidad descontada
        Assert.Equal(0, await db.OutboxCountAsync(StockReserved.EventType, sku));
        Assert.Equal(1, await db.OutboxCountAsync(StockRejected.EventType, sku));
        Assert.Equal("InsufficientStock", await db.RejectionReasonAsync(sku));           // motivo estructurado
        Assert.Equal(1, await db.InboxCountAsync(order.EventId));                        // re-entregarlo no re-decide
    }

    [Fact]
    public async Task Concurrent_orders_never_oversell()
    {
        // 5 unidades y 16 pedidos de 2 al mismo tiempo: solo caben 2 (4 unidades); sobra 1 que nadie puede tomar.
        var sku = await db.AddStockAsync(5);
        var orders = Enumerable.Range(0, 16).Select(_ => NewOrder(sku, quantity: 2)).ToList();

        await Task.WhenAll(orders.Select(order => Task.Run(() => db.ProcessAsync(order))));

        var reserved = await db.OutboxCountAsync(StockReserved.EventType, sku);
        var rejected = await db.OutboxCountAsync(StockRejected.EventType, sku);
        var available = await db.AvailableAsync(sku);

        Assert.Equal(2, reserved);
        Assert.Equal(14, rejected);
        Assert.Equal(16, reserved + rejected);          // todos los pedidos recibieron una respuesta
        Assert.Equal(1, available);                     // stock final consistente...
        Assert.Equal(5, reserved * 2 + available);      // ...y sin una sola unidad creada ni perdida
    }
}
