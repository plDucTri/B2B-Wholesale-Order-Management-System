using B2BWholesale.Portal.Data;
using B2BWholesale.Portal.Security;
using B2BWholesale.Portal.Services;
using B2BWholesale.Portal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2BWholesale.Portal.Controllers;

/// <summary>Task 3.2 - place a master-detail order. (Task 3.3 "My Orders" will add Index/Invoice here.)</summary>
[Authorize(Roles = "Agent")]
public class OrdersController : Controller
{
    private readonly PortalDbContext _db;
    private readonly IOrderService _orders;

    public OrdersController(PortalDbContext db, IOrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View(await BuildFormAsync(null, null));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(List<OrderLineInput>? lines)
    {
        lines ??= new();
        var requested = lines.Select(l => new OrderLineRequest(l.ItemId, l.Quantity));

        var result = await _orders.PlaceOrderAsync(User.GetAgentId(), requested);
        if (result.Success)
        {
            TempData["Success"] = $"Order #{result.OrderId} was placed successfully.";
            return RedirectToAction(nameof(Confirmation), new { id = result.OrderId });
        }

        // Re-show the form with the agent's quantities kept and the reasons the order was blocked.
        return View(await BuildFormAsync(lines, result.Errors));
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var agentId = User.GetAgentId();
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Agent)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(o => o.OrderID == id && o.AgentID == agentId); // agents only see their own orders

        return order is null ? NotFound() : View(order);
    }

    private async Task<OrderFormViewModel> BuildFormAsync(List<OrderLineInput>? posted, List<string>? errors)
    {
        var quantities = (posted ?? new())
            .GroupBy(l => l.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0, x.Quantity)));

        var items = await _db.Items.AsNoTracking().OrderBy(i => i.ItemName).ToListAsync();

        return new OrderFormViewModel
        {
            Errors = errors ?? new(),
            Lines = items.Select(i => new OrderFormLine
            {
                ItemId = i.ItemID,
                ItemName = i.ItemName,
                Size = i.Size,
                Price = i.Price,
                Stock = i.StockQuantity,
                Quantity = quantities.TryGetValue(i.ItemID, out var q) ? q : 0
            }).ToList()
        };
    }
}
