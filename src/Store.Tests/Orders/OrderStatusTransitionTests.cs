using Store.Core.Entities;
using Store.Core.Enums.Orders;
using Store.Core.Exceptions;

namespace Store.Tests.Orders;

/// <summary>
/// Test crítico #1: máquina de estados del pedido.
/// Pending -> Confirmed | Rejected. Los estados finales no cambian.
/// </summary>
public class OrderStatusTransitionTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

    private static Order NewOrder() => Order.Create("Ana", "ABC-01", 2, Now);

    [Fact]
    public void Create_starts_in_Pending()
    {
        var order = NewOrder();

        Assert.Equal(EOrderStatus.Pending, order.Status);
        Assert.Equal(Now, order.CreatedAt);
        Assert.Null(order.UpdatedAt);
    }

    [Fact]
    public void Confirm_from_Pending_moves_to_Confirmed()
    {
        var order = NewOrder();

        order.Confirm(Now.AddSeconds(1));

        Assert.Equal(EOrderStatus.Confirmed, order.Status);
        Assert.Equal(Now.AddSeconds(1), order.UpdatedAt);
    }

    [Fact]
    public void Reject_from_Pending_moves_to_Rejected()
    {
        var order = NewOrder();

        order.Reject(Now.AddSeconds(1));

        Assert.Equal(EOrderStatus.Rejected, order.Status);
    }

    [Theory]
    [InlineData(EOrderStatus.Confirmed, EOrderStatus.Rejected)]
    [InlineData(EOrderStatus.Rejected, EOrderStatus.Confirmed)]
    [InlineData(EOrderStatus.Confirmed, EOrderStatus.Confirmed)]
    [InlineData(EOrderStatus.Rejected, EOrderStatus.Rejected)]
    public void Final_states_cannot_transition(EOrderStatus finalState, EOrderStatus next)
    {
        var order = NewOrder();
        Apply(order, finalState);

        var ex = Assert.Throws<InvalidOrderStatusTransitionException>(() => Apply(order, next));

        Assert.Equal(finalState, ex.From);
        Assert.Equal(next, ex.To);
        Assert.Equal(finalState, order.Status);
    }

    private static void Apply(Order order, EOrderStatus status)
    {
        if (status == EOrderStatus.Confirmed) order.Confirm(Now);
        else order.Reject(Now);
    }
}
