using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Forms;

public static class UiHelper
{
    public static void StyleGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.MultiSelect = false;
        g.RowHeadersVisible = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.BackgroundColor = SystemColors.Window;
    }

    public static Button Btn(string text, EventHandler click, int width = 110)
    {
        var b = new Button { Text = text, Width = width, Height = 30, Margin = new Padding(3) };
        b.Click += click;
        return b;
    }

    public static TableLayoutPanel FieldTable()
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return t;
    }

    public static void AddRow(TableLayoutPanel t, string label, Control c)
    {
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.RowCount++;
        t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, t.RowCount - 1);
        c.Dock = DockStyle.Fill;
        c.Margin = new Padding(3, 5, 3, 3);
        t.Controls.Add(c, 1, t.RowCount - 1);
    }

    public static string Money(decimal v) => v.ToString("N0") + " VND";

    public static void Info(string msg) =>
        MessageBox.Show(msg, "B2B Wholesale", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Warn(string msg) =>
        MessageBox.Show(msg, "B2B Wholesale", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Error(string msg) =>
        MessageBox.Show(msg, "B2B Wholesale", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Confirm(string msg) =>
        MessageBox.Show(msg, "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static string DbMessage(SqlException ex) => ex.Number switch
    {
        547 => "This record is still referenced by other data (e.g. orders), so it cannot be deleted or changed this way.",
        2627 or 2601 => "A record with the same unique value (phone, username or email) already exists.",
        _ => "Database error: " + ex.Message
    };
}
