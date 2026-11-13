using System.Security.Claims;
using B2BWholesale.Portal.Data;
using B2BWholesale.Portal.Security;
using B2BWholesale.Portal.Services;
using B2BWholesale.Portal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2BWholesale.Portal.Controllers;

/// <summary>Task 3.1 - Agent login / logout.</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly PortalDbContext _db;

    public AccountController(PortalDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Agent"))
            return RedirectToAction("Index", "Catalog");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userName = model.UserName.Trim();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName);

        var result = LoginRules.Evaluate(user, model.Password);
        if (result != LoginResult.Success)
        {
            ModelState.AddModelError(string.Empty, LoginRules.Message(result));
            model.Password = "";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user!.UserID.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, "Agent"),
            new(ClaimsExtensions.AgentIdClaim, user.AgentID!.Value.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Catalog");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
