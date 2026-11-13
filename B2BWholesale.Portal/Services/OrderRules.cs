using B2BWholesale.Portal.Models;

namespace B2BWholesale.Portal.Services;

public record OrderLineRequest(int ItemId, int Quantity);

/// <summary>Pure order business rules (no database) so they can be unit-tested.</summary>
public static class OrderRules
{
    /// <summary>
    /// Drops lines with quantity &lt;= 0 (blank rows / tampering) and merges duplicate items,
    /// because OrderDetail has UNIQUE (OrderID, ItemID).
    /// </summary>
    public static List<OrderLineRequest> NormalizeLines(IEnumerable<OrderLineRequest> lines) =>
        lines.Where(l => l.Quantity > 0)
             .GroupBy(l => l.ItemId)
             .Select(g => new OrderLineRequest(g.Key, (int)Math.Min(g.Sum(x => (long)x.Quantity), int.MaxValue)))
             .OrderBy(l => l.ItemId)
             .ToList();

    public static decimal CalculateLineTotal(int quantity, decimal unitPrice) => quantity * unitPrice;

    public static decimal CalculateOrderTotal(IEnumerable<OrderLineRequest> lines, IReadOnlyDictionary<int, Item> items) =>
        lines.Where(l => items.ContainsKey(l.ItemId))
             .Sum(l => CalculateLineTotal(l.Quantity, items[l.ItemId].Price));

    /// <summary>Returns one error message per problem; an empty list means the order can be placed.</summary>
    public static List<string> CheckStock(IEnumerable<OrderLineRequest> lines, IReadOnlyDictionary<int, Item> items)
    {
        var errors = new List<string>();
        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.ItemId, out var item))
            {
                errors.Add($"Item #{line.ItemId} no longer exists.");
                continue;
            }
            if (line.Quantity > item.StockQuantity)
            {
                errors.Add($"Not enough stock for '{item.ItemName}' ({item.Size}): requested {line.Quantity}, available {item.StockQuantity}.");
            }
        }
        return errors;
    }
}
