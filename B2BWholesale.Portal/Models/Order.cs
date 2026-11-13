using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace B2BWholesale.Portal.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public int OrderID { get; set; }

    public DateTime OrderDate { get; set; }

    public int AgentID { get; set; }
    public Agent? Agent { get; set; }

    /// <summary>Pending, Shipped or Cancelled.</summary>
    [Required, MaxLength(20)]
    public string OrderStatus { get; set; } = "Pending";

    public List<OrderDetail> OrderDetails { get; set; } = new();
}
