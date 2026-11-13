# B2B Wholesale - Back-Office (WinForms) - Exercise 1

## Setup
1. SQL Server: run `database.sql`, then `sql/02_set_demo_passwords.sql`.
2. Edit `appsettings.json` if your SQL Server is not `localhost` with Windows auth.
3. Visual Studio 2022 (.NET 8 SDK): open the folder, restore NuGet, run.
4. Login: `admin` / `Admin@123` (agents use `Agent@123` but are rejected here: Admin only).

## Task map
| Task | Where |
|------|-------|
| 1.1 Secure login (Admin only, locked check, PBKDF2) | `Forms/LoginForm.cs`, `Security/PasswordHasher.cs` |
| 1.2 Items / Agents CRUD, order status | `Forms/ItemsForm.cs`, `AgentsForm.cs`, `OrdersForm.cs` |
| 1.3 Shipping Manifest (preview/print/PDF) | `Reports/ManifestPrinter.cs` |
| 1.4 Dashboard | `Forms/DashboardForm.cs` |

## Rules implemented
- Pending -> Shipped deducts stock in one transaction (fails if stock is short). Shipped/Cancelled are final.
- Revenue = Shipped orders only; best-selling = all non-cancelled orders.
- All SQL is parameterised.
- **Agree with the web-portal team**: same password format (`PasswordHasher`), and whether the portal also
  reserves stock when an order is placed (otherwise stock would be deducted twice).
