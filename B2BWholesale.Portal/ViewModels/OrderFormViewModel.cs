namespace B2BWholesale.Portal.ViewModels;

/// <summary>What the browser posts for one order line.</summary>
public class OrderLineInput
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>One row of the order form (display data always comes from the database).</summary>
public class OrderFormLine
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string Size { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int Quantity { get; set; }
}

public class OrderFormViewModel
{
    public List<OrderFormLine> Lines { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
