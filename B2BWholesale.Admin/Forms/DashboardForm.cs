using System.Data;
using B2BWholesale.Admin.Data;
using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Forms;

/// <summary>
/// Task 1.4 - Dashboard: best-selling items, low-stock warning, revenue by agent.
/// Revenue counts SHIPPED orders only. Best-selling counts every non-cancelled order (demand).
/// </summary>
public class DashboardForm : Form
{
    private readonly DateTimePicker dtpFrom = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker dtpTo = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DataGridView gridBest = new();
    private readonly DataGridView gridLow = new();
    private readonly DataGridView gridAgent = new();
    private readonly NumericUpDown numTop = new() { Minimum = 1, Maximum = 100, Value = 10, Width = 60 };
    private readonly NumericUpDown numThreshold = new() { Minimum = 0, Maximum = 1_000_000, Value = 100, Width = 80 };
    private readonly ComboBox cboAgent = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private readonly Label lblRevenue = new() { AutoSize = true, Margin = new Padding(10, 6, 30, 0) };
    private readonly Label lblPending = new() { AutoSize = true, Margin = new Padding(10, 6, 30, 0) };
    private readonly Label lblLow = new() { AutoSize = true, Margin = new Padding(10, 6, 30, 0), ForeColor = Color.Firebrick };
    private readonly Label lblAgentTotal = new() { AutoSize = true, Margin = new Padding(20, 6, 0, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private bool ready;

    public DashboardForm()
    {
        Text = "Dashboard";
        dtpFrom.Value = DateTime.Today.AddMonths(-6);
        dtpTo.Value = DateTime.Today;
        foreach (var g in new[] { gridBest, gridLow, gridAgent }) UiHelper.StyleGrid(g);

        // --- tabs ---
        var tabs = new TabControl { Dock = DockStyle.Fill };

        var tabBest = new TabPage("Best-selling items");
        var barBest = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6, 6, 0, 0) };
        barBest.Controls.Add(new Label { Text = "Show top", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        barBest.Controls.Add(numTop);
        barBest.Controls.Add(new Label { Text = "items (by units sold, non-cancelled orders)", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        tabBest.Controls.Add(gridBest);
        tabBest.Controls.Add(barBest);

        var tabLow = new TabPage("Low-stock warning");
        var barLow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6, 6, 0, 0) };
        barLow.Controls.Add(new Label { Text = "Warn when stock <=", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        barLow.Controls.Add(numThreshold);
        barLow.Controls.Add(new Label { Text = "(CRITICAL = at or below half the threshold)", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        tabLow.Controls.Add(gridLow);
        tabLow.Controls.Add(barLow);

        var tabAgent = new TabPage("Revenue by agent");
        var barAgent = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6, 6, 0, 0) };
        barAgent.Controls.Add(new Label { Text = "Agent:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        barAgent.Controls.Add(cboAgent);
        barAgent.Controls.Add(lblAgentTotal);
        tabAgent.Controls.Add(gridAgent);
        tabAgent.Controls.Add(barAgent);

        tabs.TabPages.AddRange(new[] { tabBest, tabLow, tabAgent });

        // --- KPI strip + date range ---
        var kpi = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, BackColor = Color.WhiteSmoke };
        var bold = new Font("Segoe UI", 10, FontStyle.Bold);
        lblRevenue.Font = lblPending.Font = lblLow.Font = bold;
        kpi.Controls.AddRange(new Control[] { lblRevenue, lblPending, lblLow });

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 8, 0, 0) };
        top.Controls.Add(new Label { Text = "From:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        top.Controls.Add(dtpFrom);
        top.Controls.Add(new Label { Text = "To:", AutoSize = true, Margin = new Padding(12, 6, 3, 0) });
        top.Controls.Add(dtpTo);
        top.Controls.Add(UiHelper.Btn("Refresh", (_, _) => RefreshAll(), 90));

        Controls.Add(tabs);
        Controls.Add(kpi);
        Controls.Add(top);

        Load += (_, _) =>
        {
            LoadAgentCombo();
            ready = true;
            RefreshAll();
        };

        void Changed(object? s, EventArgs e) { if (ready) RefreshAll(); }
        dtpFrom.ValueChanged += Changed;
        dtpTo.ValueChanged += Changed;
        numTop.ValueChanged += Changed;
        numThreshold.ValueChanged += Changed;
        cboAgent.SelectedIndexChanged += Changed;
    }

    private void LoadAgentCombo()
    {
        var dt = Db.Query("SELECT AgentID, AgentName FROM Agent ORDER BY AgentName");
        var all = dt.NewRow();
        all["AgentID"] = DBNull.Value;
        all["AgentName"] = "(All agents)";
        dt.Rows.InsertAt(all, 0);
        cboAgent.DisplayMember = "AgentName";
        cboAgent.ValueMember = "AgentID";
        cboAgent.DataSource = dt;
    }

    private (SqlParameter From, SqlParameter To) DateParams() =>
        (Db.P("@f", dtpFrom.Value.Date), Db.P("@t", dtpTo.Value.Date.AddDays(1))); // end exclusive

    private void RefreshAll()
    {
        if (dtpFrom.Value.Date > dtpTo.Value.Date)
        {
            UiHelper.Warn("'From' date must not be after 'To' date.");
            return;
        }

        try
        {
            // 1) best-selling
            var (f, t) = DateParams();
            gridBest.DataSource = Db.Query(
                "SELECT TOP (@n) i.ItemID, i.ItemName, i.Size, SUM(od.Quantity) AS UnitsSold, " +
                "       SUM(od.Quantity * od.UnitAmount) AS Revenue " +
                "FROM OrderDetail od JOIN Orders o ON o.OrderID = od.OrderID JOIN Item i ON i.ItemID = od.ItemID " +
                "WHERE o.OrderStatus <> 'Cancelled' AND o.OrderDate >= @f AND o.OrderDate < @t " +
                "GROUP BY i.ItemID, i.ItemName, i.Size ORDER BY UnitsSold DESC, Revenue DESC",
                Db.P("@n", (int)numTop.Value), f, t);
            if (gridBest.Columns.Contains("Revenue")) gridBest.Columns["Revenue"]!.DefaultCellStyle.Format = "N0";

            // 2) low stock
            var low = Db.Query(
                "SELECT ItemID, ItemName, Size, StockQuantity, " +
                "       CASE WHEN StockQuantity <= @th / 2 THEN 'CRITICAL' ELSE 'LOW' END AS Level " +
                "FROM Item WHERE StockQuantity <= @th ORDER BY StockQuantity, ItemName",
                Db.P("@th", (int)numThreshold.Value));
            gridLow.DataSource = low;
            foreach (DataGridViewRow r in gridLow.Rows)
            {
                r.DefaultCellStyle.BackColor = (string)r.Cells["Level"].Value == "CRITICAL"
                    ? Color.MistyRose : Color.LightYellow;
            }

            // 3) revenue by agent (shipped orders only)
            var (f2, t2) = DateParams();
            var agentParam = new SqlParameter("@a", SqlDbType.Int)
            {
                Value = cboAgent.SelectedValue is int aid ? aid : DBNull.Value
            };
            var rev = Db.Query(
                "SELECT a.AgentID, a.AgentName, COUNT(DISTINCT o.OrderID) AS ShippedOrders, " +
                "       ISNULL(SUM(od.Quantity * od.UnitAmount), 0) AS Revenue " +
                "FROM Agent a " +
                "LEFT JOIN Orders o ON o.AgentID = a.AgentID AND o.OrderStatus = 'Shipped' " +
                "     AND o.OrderDate >= @f AND o.OrderDate < @t " +
                "LEFT JOIN OrderDetail od ON od.OrderID = o.OrderID " +
                "WHERE (@a IS NULL OR a.AgentID = @a) " +
                "GROUP BY a.AgentID, a.AgentName ORDER BY Revenue DESC, a.AgentName",
                f2, t2, agentParam);
            gridAgent.DataSource = rev;
            gridAgent.Columns["Revenue"]!.DefaultCellStyle.Format = "N0";
            lblAgentTotal.Text = "Total: " + UiHelper.Money(rev.AsEnumerable().Sum(r => r.Field<decimal>("Revenue")));

            // KPI strip
            var (f3, t3) = DateParams();
            var totalRev = Convert.ToDecimal(Db.Scalar(
                "SELECT ISNULL(SUM(od.Quantity * od.UnitAmount), 0) FROM Orders o " +
                "JOIN OrderDetail od ON od.OrderID = o.OrderID " +
                "WHERE o.OrderStatus = 'Shipped' AND o.OrderDate >= @f AND o.OrderDate < @t", f3, t3) ?? 0m);
            var pending = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM Orders WHERE OrderStatus = 'Pending'") ?? 0);
            lblRevenue.Text = "Revenue (shipped, in range): " + UiHelper.Money(totalRev);
            lblPending.Text = "Pending orders: " + pending;
            lblLow.Text = low.Rows.Count > 0 ? $"\u26A0 {low.Rows.Count} item(s) low on stock" : "Stock levels OK";
        }
        catch (Exception ex) { UiHelper.Error(ex.Message); }
    }
}
