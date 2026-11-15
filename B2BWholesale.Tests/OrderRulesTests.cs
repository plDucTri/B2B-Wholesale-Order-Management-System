using System;
using System.Collections.Generic;
using System.Text;
using B2BWholesale.Portal.Models;
using B2BWholesale.Portal.Services;
using Xunit;

namespace B2BWholesale.Tests;

public class OrderRulesTests
{
    private static Item MakeItem(int id, string name, decimal price, int stock, string size = "M") =>
        new Item { ItemID = id, ItemName = name, Price = price, StockQuantity = stock, Size = size };

    private static Dictionary<int, Item> Catalog(params Item[] items) =>
        items.ToDictionary(i => i.ItemID);

    // ---------- NormalizeLines ----------

    [Fact]
    public void NormalizeLines_DropsZeroAndNegativeQuantities()
    {
        var input = new[]
        {
            new OrderLineRequest(1, 0),
            new OrderLineRequest(2, -5),
            new OrderLineRequest(3, 4)
        };

        var result = OrderRules.NormalizeLines(input);

        var line = Assert.Single(result);
        Assert.Equal(3, line.ItemId);
        Assert.Equal(4, line.Quantity);
    }

    [Fact]
    public void NormalizeLines_MergesDuplicateItems()
    {
        var input = new[]
        {
            new OrderLineRequest(7, 3),
            new OrderLineRequest(7, 5),
            new OrderLineRequest(2, 1)
        };

        var result = OrderRules.NormalizeLines(input);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].ItemId);          // sorted by ItemId
        Assert.Equal(7, result[1].ItemId);
        Assert.Equal(8, result[1].Quantity);        // 3 + 5
    }

    [Fact]
    public void NormalizeLines_HugeQuantities_DoNotOverflow()
    {
        var input = new[]
        {
            new OrderLineRequest(1, int.MaxValue),
            new OrderLineRequest(1, int.MaxValue)
        };

        var result = OrderRules.NormalizeLines(input);

        Assert.Equal(int.MaxValue, Assert.Single(result).Quantity);
    }

    // ---------- Totals ----------

    [Fact]
    public void CalculateLineTotal_MultipliesQuantityByPrice()
    {
        Assert.Equal(75.50m, OrderRules.CalculateLineTotal(5, 15.10m));
    }

    [Fact]
    public void CalculateOrderTotal_SumsAllLines()
    {
        var items = Catalog(
            MakeItem(1, "Widget", 10m, 100),
            MakeItem(2, "Gadget", 2.50m, 100));
        var lines = new[]
        {
            new OrderLineRequest(1, 3),   // 30
            new OrderLineRequest(2, 4)    // 10
        };

        Assert.Equal(40m, OrderRules.CalculateOrderTotal(lines, items));
    }

    [Fact]
    public void CalculateOrderTotal_IgnoresItemsNotInCatalog()
    {
        var items = Catalog(MakeItem(1, "Widget", 10m, 100));
        var lines = new[]
        {
            new OrderLineRequest(1, 2),    // 20
            new OrderLineRequest(99, 5)    // missing -> ignored
        };

        Assert.Equal(20m, OrderRules.CalculateOrderTotal(lines, items));
    }

    // ---------- Stock checks ----------

    [Fact]
    public void CheckStock_EnoughStock_ReturnsNoErrors()
    {
        var items = Catalog(MakeItem(1, "Widget", 10m, 50));

        var errors = OrderRules.CheckStock(new[] { new OrderLineRequest(1, 20) }, items);

        Assert.Empty(errors);
    }

    [Fact]
    public void CheckStock_OrderingExactlyAllStock_IsAllowed()
    {
        var items = Catalog(MakeItem(1, "Widget", 10m, 5));

        var errors = OrderRules.CheckStock(new[] { new OrderLineRequest(1, 5) }, items);

        Assert.Empty(errors);
    }

    [Fact]
    public void CheckStock_InsufficientStock_IsBlockedWithClearMessage()
    {
        var items = Catalog(MakeItem(1, "Widget", 10m, 5));

        var errors = OrderRules.CheckStock(new[] { new OrderLineRequest(1, 6) }, items);

        var message = Assert.Single(errors);
        Assert.Contains("Widget", message);
        Assert.Contains("requested 6", message);
        Assert.Contains("available 5", message);
    }

    [Fact]
    public void CheckStock_MissingItem_ReportsItemNoLongerExists()
    {
        var items = Catalog(MakeItem(1, "Widget", 10m, 5));

        var errors = OrderRules.CheckStock(new[] { new OrderLineRequest(42, 1) }, items);

        Assert.Contains("#42", Assert.Single(errors));
    }

    [Fact]
    public void CheckStock_MultipleProblems_AreAllReported()
    {
        var items = Catalog(
            MakeItem(1, "Widget", 10m, 2),
            MakeItem(2, "Gadget", 5m, 100));
        var lines = new[]
        {
            new OrderLineRequest(1, 10),   // too many
            new OrderLineRequest(2, 10),   // fine
            new OrderLineRequest(3, 1)     // missing
        };

        var errors = OrderRules.CheckStock(lines, items);

        Assert.Equal(2, errors.Count);
    }
}