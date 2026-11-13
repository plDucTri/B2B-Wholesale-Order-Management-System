using B2BWholesale.Portal.Data;
using B2BWholesale.Portal.Models;
using Microsoft.EntityFrameworkCore;

namespace B2BWholesale.Portal.Services;

public record PlaceOrderResult(bool Success, int? OrderId, decimal Total, List<string> Errors)
{
    public static PlaceOrderResult Ok(int orderId, decimal total) => new(true, orderId, total, new());
    public static PlaceOrderResult Fail(params string[] errors) => new(false, null, 0, errors.ToList());
    public static PlaceOrderResult Fail(List<string> errors) => new(false, null, 0, errors);
}

public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(int agentId, IEnumerable<OrderLineRequest> lines);
}

public class OrderService : IOrderService
{
    private readonly PortalDbContext _db;
    private readonly ILogger<OrderService> _log;

    public OrderService(PortalDbContext db, ILogger<OrderService> log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>
    /// Task 3.2 core logic. ONE database transaction that
    ///   1) inserts the Orders row,
    ///   2) inserts the OrderDetail rows,
    ///   3) deducts the ordered quantities from Item.StockQuantity.
    /// If any line has insufficient stock, nothing is saved.
    /// </summary>
    public async Task<PlaceOrderResult> PlaceOrderAsync(int agentId, IEnumerable<OrderLineRequest> requested)
    {
        var lines = OrderRules.NormalizeLines(requested);
        if (lines.Count == 0)
            return PlaceOrderResult.Fail("Please enter a quantity greater than 0 for at least one item.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (!await _db.Agents.AnyAsync(a => a.AgentID == agentId))
                return PlaceOrderResult.Fail("Agent profile not found.");

            // Prices come from the database, never from the browser.
            var ids = lines.Select(l => l.ItemId).ToList();
            var items = await _db.Items.AsNoTracking()
                .Where(i => ids.Contains(i.ItemID))
                .ToDictionaryAsync(i => i.ItemID);

            // Friendly pre-check: reports every problem line at once.
            var errors = OrderRules.CheckStock(lines, items);
            if (errors.Count > 0)
                return PlaceOrderResult.Fail(errors);

            // 1 + 2) Order header and its details are inserted in one SaveChanges.
            var order = new Order
            {
                OrderDate = DateTime.Now,
                AgentID = agentId,
                OrderStatus = "Pending",
                OrderDetails = lines.Select(l => new OrderDetail
                {
                    ItemID = l.ItemId,
                    Quantity = l.Quantity,
                    UnitAmount = items[l.ItemId].Price
                }).ToList()
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 3) Atomic, race-safe deduction: the WHERE clause refuses to go below zero,
            //    so two agents buying the last units at the same time cannot both succeed.
            foreach (var l in lines)
            {
                var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Item SET StockQuantity = StockQuantity - {l.Quantity} WHERE ItemID = {l.ItemId} AND StockQuantity >= {l.Quantity}");

                if (affected != 1)
                {
                    // Returning without Commit => the transaction is rolled back on dispose.
                    return PlaceOrderResult.Fail(
                        $"'{items[l.ItemId].ItemName}' just ran low on stock while your order was being placed. Nothing was saved - please review the quantities and try again.");
                }
            }

            await tx.CommitAsync();
            return PlaceOrderResult.Ok(order.OrderID, OrderRules.CalculateOrderTotal(lines, items));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Placing order failed for agent {AgentId}", agentId);
            return PlaceOrderResult.Fail("Something went wrong while placing the order. Nothing was saved - please try again.");
        }
    }
}
