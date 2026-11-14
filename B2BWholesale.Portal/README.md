# B2B Wholesale - Agent Portal (Tasks 3.1 + 3.2)

ASP.NET Core 8 MVC. Shares the SQL Server database `WholesaleManagementDB` with the WinForms back-office app.

## Setup
1. SQL Server: run `database.sql`, then `sql/02_set_demo_passwords.sql` (from the WinForms project).
2. Edit `appsettings.json` -> `ConnectionStrings:DefaultConnection` if your server is not `localhost` with Windows auth.
3. `dotnet restore` then `dotnet run` (or press F5 in Visual Studio 2022).
4. Login: `agent01` / `Agent@123` (admin accounts are rejected by the portal).

## Where things are
| Task | Files |
|------|-------|
| 3.1 Login (Agent only, locked check, PBKDF2, cookie auth) | `Controllers/AccountController.cs`, `Services/LoginRules.cs`, `Security/PasswordHasher.cs`, `Views/Account/*` |
| 3.2 Catalog | `Controllers/CatalogController.cs`, `Views/Catalog/Index.cshtml` |
| 3.2 Master-detail order + DB transaction + stock deduction | `Services/OrderService.cs`, `Services/OrderRules.cs`, `Controllers/OrdersController.cs`, `Views/Orders/*` |

## Business rules
- Stock is deducted when the agent places the order (inside the same transaction as the Order/OrderDetail inserts).
- If any item has insufficient stock the whole order is rolled back.
- Unit prices are read from the database on the server, not from the browser.
