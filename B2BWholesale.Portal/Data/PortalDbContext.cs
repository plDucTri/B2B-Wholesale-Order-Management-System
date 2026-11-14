using B2BWholesale.Portal.Models;
using Microsoft.EntityFrameworkCore;

namespace B2BWholesale.Portal.Data;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // The configuration below mirrors database.sql so that the model, the migrations
        // and the script all describe the same schema.
        b.Entity<Item>().ToTable("Item", t =>
        {
            t.HasCheckConstraint("CK_Item_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Item_Stock", "[StockQuantity] >= 0");
        });

        b.Entity<Agent>().HasIndex(a => a.Phone).IsUnique();

        b.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin', 'Agent')"));
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Agent).WithMany().HasForeignKey(u => u.AgentID).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("Orders", t => t.HasCheckConstraint("CK_Orders_Status", "[OrderStatus] IN ('Pending', 'Shipped', 'Cancelled')"));
            e.Property(o => o.OrderDate).HasColumnType("datetime2");
            e.HasOne(o => o.Agent).WithMany().HasForeignKey(o => o.AgentID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<OrderDetail>(e =>
        {
            e.ToTable("OrderDetail", t =>
            {
                t.HasCheckConstraint("CK_OrderDetail_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_OrderDetail_UnitAmount", "[UnitAmount] > 0");
            });
            e.HasIndex(d => new { d.OrderID, d.ItemID }).IsUnique();
            e.HasOne(d => d.Order).WithMany(o => o.OrderDetails).HasForeignKey(d => d.OrderID).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Item).WithMany().HasForeignKey(d => d.ItemID).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
