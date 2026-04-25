using Sdmp.Monolith.Domain;
using Xunit;

namespace Sdmp.Monolith.Tests;

public class OrderDomainTests
{
    [Fact]
    public void OrderLine_LineTotal_multiplies_quantity_by_unit_price()
    {
        var line = new OrderLine(Guid.NewGuid(), Quantity: 3, UnitPrice: 9.99m);
        Assert.Equal(29.97m, line.LineTotal);
    }

    [Fact]
    public void Order_Total_sums_all_line_totals()
    {
        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[]
            {
                new OrderLine(Guid.NewGuid(), 2, 100m),
                new OrderLine(Guid.NewGuid(), 1, 49.50m)
            },
            OrderStatus.Pending,
            DateTimeOffset.UtcNow);

        Assert.Equal(249.50m, order.Total);
    }

    [Fact]
    public void Order_Total_is_zero_for_no_lines()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<OrderLine>(),
            OrderStatus.Pending, DateTimeOffset.UtcNow);
        Assert.Equal(0m, order.Total);
    }
}
