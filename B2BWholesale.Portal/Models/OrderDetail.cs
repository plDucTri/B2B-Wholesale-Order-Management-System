using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace B2BWholesale.Portal.Models;

[Table("OrderDetail")]
public class OrderDetail
{
    [Key]
    public int ID { get; set; }

    public int OrderID { get; set; }
    public Order? Order { get; set; }

    public int ItemID { get; set; }
    public Item? Item { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitAmount { get; set; }

    [NotMapped]
    public decimal LineTotal => Quantity * UnitAmount;
}
