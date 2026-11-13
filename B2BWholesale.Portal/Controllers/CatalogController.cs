using B2BWholesale.Portal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2BWholesale.Portal.Controllers;

/// <summary>Task 3.2 - catalog of items.</summary>
[Authorize(Roles = "Agent")]
public class CatalogController : Controller
{
    private readonly PortalDbContext _db;

    public CatalogController(PortalDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Items.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i => i.ItemName.Contains(term) || i.Size.Contains(term));
        }

        ViewData["Query"] = q;
        return View(await query.OrderBy(i => i.ItemName).ToListAsync());
    }
}
