using System.Data;
using B2BWholesale.Admin.Data;
using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Forms;

/// <summary>Task 1.2 - CRUD for Items, including atomic stock adjustment.</summary>
public class ItemsForm : Form
{
    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new() { Width = 240 };
    private readonly TextBox txtName = new();
    private readonly TextBox txtSize = new();
    private readonly NumericUpDown numPrice = new() { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1_000_000_000m, ThousandsSeparator = true, Value = 1000 };
    private readonly NumericUpDown numStock = new() { Maximum = 10_000_000 };
    private readonly NumericUpDown numAdjust = new() { Minimum = -1_000_000, Maximum = 1_000_000 };
    private int? selectedId;
    private bool loading;

    public ItemsForm()
    {
        Text = "Item Management";
        UiHelper.StyleGrid(grid);
        grid.SelectionChanged += (_, _) => OnRowSelected();

        // search bar
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 8, 0, 0) };
        top.Controls.Add(new Label { Text = "Search:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        top.Controls.Add(txtSearch);
        txtSearch.TextChanged += (_, _) => LoadData();

        // right edit panel (buttons added first so the field table ends up above them)
        var right = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(10) };

        var adjust = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        adjust.Controls.Add(new Label { Text = "Stock +/-", AutoSize = true, Margin = new Padding(3, 8, 3, 0) });
        numAdjust.Width = 90;
        adjust.Controls.Add(numAdjust);
        adjust.Controls.Add(UiHelper.Btn("Apply", (_, _) => AdjustStock(), 70));

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 10, 0, 10) };
        buttons.Controls.Add(UiHelper.Btn("New", (_, _) => ClearForm(), 70));
        buttons.Controls.Add(UiHelper.Btn("Add", (_, _) => AddItem(), 70));
        buttons.Controls.Add(UiHelper.Btn("Update", (_, _) => UpdateItem(), 70));
        buttons.Controls.Add(UiHelper.Btn("Delete", (_, _) => DeleteItem(), 70));

        var fields = UiHelper.FieldTable();
        UiHelper.AddRow(fields, "Name", txtName);
        UiHelper.AddRow(fields, "Size", txtSize);
        UiHelper.AddRow(fields, "Price", numPrice);
        UiHelper.AddRow(fields, "Stock", numStock);

        right.Controls.Add(adjust);
        right.Controls.Add(buttons);
        right.Controls.Add(fields);

        Controls.Add(grid);
        Controls.Add(right);
        Controls.Add(top);

        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            var keep = selectedId;
            var like = "%" + txtSearch.Text.Trim() + "%";
            loading = true;
            grid.DataSource = Db.Query(
                "SELECT ItemID, ItemName, Size, Price, StockQuantity FROM Item " +
                "WHERE ItemName LIKE @s OR Size LIKE @s ORDER BY ItemID", Db.P("@s", like));
            loading = false;

            if (grid.Columns.Contains("Price")) grid.Columns["Price"]!.DefaultCellStyle.Format = "N0";
            if (keep != null)
            {
                foreach (DataGridViewRow r in grid.Rows)
                {
                    if ((int)r.Cells["ItemID"].Value == keep) { grid.CurrentCell = r.Cells["ItemName"]; break; }
                }
            }
            OnRowSelected();
        }
        catch (Exception ex) { loading = false; UiHelper.Error(ex.Message); }
    }

    private void OnRowSelected()
    {
        if (loading || grid.CurrentRow == null) return;
        var r = grid.CurrentRow;
        selectedId = Convert.ToInt32(r.Cells["ItemID"].Value);
        txtName.Text = r.Cells["ItemName"].Value?.ToString() ?? "";
        txtSize.Text = r.Cells["Size"].Value?.ToString() ?? "";
        numPrice.Value = Convert.ToDecimal(r.Cells["Price"].Value);
        numStock.Value = Convert.ToDecimal(r.Cells["StockQuantity"].Value);
    }

    private void ClearForm()
    {
        selectedId = null;
        txtName.Clear(); txtSize.Clear();
        numPrice.Value = 1000; numStock.Value = 0; numAdjust.Value = 0;
        grid.ClearSelection();
        txtName.Focus();
    }

    private bool ReadInput(out string name, out string size)
    {
        name = txtName.Text.Trim();
        size = txtSize.Text.Trim();
        if (name.Length == 0 || size.Length == 0)
        {
            UiHelper.Warn("Item name and size are required.");
            return false;
        }
        return true;
    }

    private void AddItem()
    {
        if (!ReadInput(out var name, out var size)) return;
        try
        {
            Db.Execute("INSERT INTO Item (ItemName, Size, Price, StockQuantity) VALUES (@n, @s, @p, @q)",
                Db.P("@n", name), Db.P("@s", size), Db.P("@p", numPrice.Value), Db.P("@q", (int)numStock.Value));
            selectedId = null;
            LoadData();
            UiHelper.Info("Item added.");
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    private void UpdateItem()
    {
        if (selectedId == null) { UiHelper.Warn("Select an item first."); return; }
        if (!ReadInput(out var name, out var size)) return;
        try
        {
            Db.Execute("UPDATE Item SET ItemName=@n, Size=@s, Price=@p, StockQuantity=@q WHERE ItemID=@id",
                Db.P("@n", name), Db.P("@s", size), Db.P("@p", numPrice.Value),
                Db.P("@q", (int)numStock.Value), Db.P("@id", selectedId));
            LoadData();
            UiHelper.Info("Item updated.");
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    private void DeleteItem()
    {
        if (selectedId == null) { UiHelper.Warn("Select an item first."); return; }
        if (!UiHelper.Confirm($"Delete item '{txtName.Text}'?")) return;
        try
        {
            Db.Execute("DELETE FROM Item WHERE ItemID=@id", Db.P("@id", selectedId));
            selectedId = null;
            LoadData();
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    /// <summary>Receive stock (+) or write off (-) in one atomic statement; never goes below zero.</summary>
    private void AdjustStock()
    {
        if (selectedId == null) { UiHelper.Warn("Select an item first."); return; }
        var delta = (int)numAdjust.Value;
        if (delta == 0) return;
        try
        {
            var rows = Db.Execute(
                "UPDATE Item SET StockQuantity = StockQuantity + @d WHERE ItemID=@id AND StockQuantity + @d >= 0",
                Db.P("@d", delta), Db.P("@id", selectedId));
            if (rows == 0) { UiHelper.Warn("Not enough stock for that reduction."); return; }
            numAdjust.Value = 0;
            LoadData();
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }
}
