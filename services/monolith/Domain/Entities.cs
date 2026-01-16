namespace Sdmp.Monolith.Domain;

/// <summary>
/// Marker for an aggregate root with a stable identity. Keeping this explicit makes the Phase 3
/// extraction (one aggregate → one service) a mechanical move rather than a redesign.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}

public sealed record User(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt) : IEntity;

public sealed record Product(Guid Id, string Sku, string Name, decimal Price, int Stock) : IEntity;

public enum OrderStatus { Pending, Paid, Shipped, Cancelled }

public sealed record OrderLine(Guid ProductId, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed record Order(
    Guid Id,
    Guid UserId,
    IReadOnlyList<OrderLine> Lines,
    OrderStatus Status,
    DateTimeOffset CreatedAt) : IEntity
{
    public decimal Total => Lines.Sum(l => l.LineTotal);
}
