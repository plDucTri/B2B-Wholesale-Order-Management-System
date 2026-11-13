using System.Data;
using B2BWholesale.Admin.Data;
using B2BWholesale.Admin.Reports;
using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Forms;

/// <summary>
/// Task 1.2 (orders) + Task 1.3 (manifest). Lists incoming web orders, shows their lines,
/// updates status and prints the shipping manifest.
/// Business rule: stock is deducted when the agent places the order (web portal, Task 3.2).
/// Pending -> Shipped only changes the status; Pending -> Cancelled returns the stock; Shipped/Cancelled are final.
/// </summary>
public class OrdersForm : Form
{
    private readonly DataGridView gridOrders = new();
    private readonly DataGridView gridLines = new();
    private readonly ComboBox cboFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly TextBox txtAgent = new() { Width = 200 };
    private readonly ComboBox cboNewStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly Button btnUpdate;
    private readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
    private bool loading;

    public OrdersForm()
    {
        Text = "Order Management";
        UiHelper.StyleGrid(gridOrders);
        UiHelper.StyleGrid(gridLines);
        gridOrders.SelectionChanged += (_, _) => OnOrderSelected();

        split.Panel1.Controls.Add(gridOrders);
        split.Panel2.Controls.Add(gridLines);

        // filter bar
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 8, 0, 0) };
        cboFilter.Items.AddRange(new object[] { "All", "Pending", "Shipped", "Cancelled" });
        cboFilter.SelectedIndex = 1; // Pending first: that's the warehouse's work queue
        top.Controls.Add(new Label { Text = "Status:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        top.Controls.Add(cboFilter);
        top.Controls.Add(new Label { Text = "Agent:", AutoSize = true, Margin = new Padding(12, 6, 3, 0) });
        top.Controls.Add(txtAgent);
        top.Controls.Add(UiHelper.Btn("Refresh", (_, _) => LoadOrders(), 80));
        cboFilter.SelectedIndexChanged += (_, _) => LoadOrders();
        txtAgent.TextChanged += (_, _) => LoadOrders();

        // action bar
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8, 8, 0, 0) };
        bottom.Controls.Add(new Label { Text = "Set status:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        cboNewStatus.Items.AddRange(new object[] { "Shipped", "Cancelled" });
        cboNewStatus.SelectedIndex = 0;
        bottom.Controls.Add(cboNewStatus);
        btnUpdate = UiHelper.Btn("Update status", (_, _) => UpdateStatus(), 120);
        bottom.Controls.Add(btnUpdate);
        bottom.Controls.Add(UiHelper.Btn("Print manifest", (_, _) => Manifest(false), 120));
        bottom.Controls.Add(UiHelper.Btn("Export PDF", (_, _) => Manifest(true), 100));

        Controls.Add(split);
        Controls.Add(top);
        Controls.Add(bottom);

        Load += (_, _) => LoadOrders();
        Shown += (_, _) => { try { split.SplitterDistance = split.Height * 55 / 100; } catch { } };
    }

    private int? CurrentOrderId =>
        gridOrders.CurrentRow == null ? null : Convert.ToInt32(gridOrders.CurrentRow.Cells["OrderID"].Value);

    private void LoadOrders()
    {
        try
        {
            var keep = CurrentOrderId;
            loading = true;
            gridOrders.DataSource = Db.Query(
                "SELECT o.OrderID, o.OrderDate, a.AgentName, o.OrderStatus, " +
                "       ISNULL(SUM(od.Quantity * od.UnitAmount), 0) AS Total " +
                "FROM Orders o JOIN Agent a ON a.AgentID = o.AgentID " +
                "LEFT JOIN OrderDetail od ON od.OrderID = o.OrderID " +
                "WHERE (@st = 'All' OR o.OrderStatus = @st) AND a.AgentName LIKE @q " +
                "GROUP BY o.OrderID, o.OrderDate, a.AgentName, o.OrderStatus " +
                "ORDER BY CASE WHEN o.OrderStatus = 'Pending' THEN 0 ELSE 1 END, o.OrderDate DESC",
                Db.P("@st", cboFilter.Text), Db.P("@q", "%" + txtAgent.Text.Trim() + "%"));
            loading = false;

            gridOrders.Columns["Total"]!.DefaultCellStyle.Format = "N0";
            gridOrders.Columns["OrderDate"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
            if (keep != null)
            {
                foreach (DataGridViewRow r in gridOrders.Rows)
                {
                    if ((int)r.Cells["OrderID"].Value == keep) { gridOrders.CurrentCell = r.Cells["OrderID"]; break; }
                }
            }
            OnOrderSelected();
        }
        catch (Exception ex) { loading = false; UiHelper.Error(ex.Message); }
    }

    private void OnOrderSelected()
    {
        if (loading) return;
        if (CurrentOrderId is not int id)
        {
            gridLines.DataSource = null;
            btnUpdate.Enabled = false;
            return;
        }

        gridLines.DataSource = Db.Query(
            "SELECT od.ID, i.ItemName, i.Size, od.Quantity, od.UnitAmount, " +
            "       od.Quantity * od.UnitAmount AS Subtotal, i.StockQuantity AS InStock " +
            "FROM OrderDetail od JOIN Item i ON i.ItemID = od.ItemID WHERE od.OrderID = @id ORDER BY od.ID",
            Db.P("@id", id));
        gridLines.Columns["UnitAmount"]!.DefaultCellStyle.Format = "N0";
        gridLines.Columns["Subtotal"]!.DefaultCellStyle.Format = "N0";

        // only Pending orders can change status
        btnUpdate.Enabled = (string)gridOrders.CurrentRow!.Cells["OrderStatus"].Value == "Pending";
    }

    private void UpdateStatus()
    {
        if (CurrentOrderId is not int id) { UiHelper.Warn("Select an order first."); return; }
        var newStatus = cboNewStatus.Text;
        if (!UiHelper.Confirm($"Mark order #{id} as {newStatus}?" +
                              (newStatus == "Cancelled" ? "\nThe ordered quantities will be returned to stock." : ""))) return;

        try
        {
            using var con = Db.Open();
            using var tx = con.BeginTransaction();
            try
            {
                var current = (string?)Db.Cmd(con, tx,
                    "SELECT OrderStatus FROM Orders WITH (UPDLOCK) WHERE OrderID=@id", Db.P("@id", id)).ExecuteScalar();
                if (current != "Pending")
                    throw new InvalidOperationException($"Order is already {current}; its status can no longer change.");

                // Stock was already deducted when the agent placed the order on the portal, so
                // Shipped changes nothing in Item. Cancelled must give that stock back.
                if (newStatus == "Cancelled")
                {
                    var lines = new List<(int ItemId, int Qty)>();
                    using (var rd = Db.Cmd(con, tx,
                        "SELECT ItemID, Quantity FROM OrderDetail WHERE OrderID=@id", Db.P("@id", id)).ExecuteReader())
                    {
                        while (rd.Read()) lines.Add((rd.GetInt32(0), rd.GetInt32(1)));
                    }
                    foreach (var l in lines)
                    {
                        Db.Cmd(con, tx,
                            "UPDATE Item SET StockQuantity = StockQuantity + @q WHERE ItemID=@i",
                            Db.P("@q", l.Qty), Db.P("@i", l.ItemId)).ExecuteNonQuery();
                    }
                }

                Db.Cmd(con, tx, "UPDATE Orders SET OrderStatus=@s WHERE OrderID=@id",
                    Db.P("@s", newStatus), Db.P("@id", id)).ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }

            LoadOrders();
        }
        catch (InvalidOperationException ex) { UiHelper.Warn(ex.Message); }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    private void Manifest(bool pdf)
    {
        if (CurrentOrderId is not int id) { UiHelper.Warn("Select an order first."); return; }
        try
        {
            if (pdf) ManifestPrinter.ExportPdf(id, this);
            else ManifestPrinter.Preview(id, this);
        }
        catch (Exception ex) { UiHelper.Error(ex.Message); }
    }
}
