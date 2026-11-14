using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace B2BWholesale.Portal.Models;

[Table("Item")]
public class Item
{
    [Key]
    public int ItemID { get; set; }

    [Required, MaxLength(150)]
    public string ItemName { get; set; } = "";

    [Required, MaxLength(50)]
    public string Size { get; set; } = "";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}
