using System.Text.RegularExpressions;
using B2BWholesale.Admin.Data;
using B2BWholesale.Admin.Security;
using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Forms;

/// <summary>Task 1.2 - register / edit / delete agents, optionally creating their Agent Portal login.</summary>
public class AgentsForm : Form
{
    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new() { Width = 240 };
    private readonly TextBox txtName = new();
    private readonly TextBox txtAddress = new();
    private readonly TextBox txtPhone = new();
    private readonly CheckBox chkAccount = new() { Text = "Create portal login (new agents)" };
    private readonly TextBox txtUser = new() { Enabled = false };
    private readonly TextBox txtEmail = new() { Enabled = false };
    private readonly TextBox txtPass = new() { Enabled = false, UseSystemPasswordChar = true };
    private int? selectedId;
    private bool loading;

    public AgentsForm()
    {
        Text = "Agent Management";
        UiHelper.StyleGrid(grid);
        grid.SelectionChanged += (_, _) => OnRowSelected();

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(8, 8, 0, 0) };
        top.Controls.Add(new Label { Text = "Search:", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
        top.Controls.Add(txtSearch);
        txtSearch.TextChanged += (_, _) => LoadData();

        var right = new Panel { Dock = DockStyle.Right, Width = 360, Padding = new Padding(10) };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 10, 0, 0) };
        buttons.Controls.Add(UiHelper.Btn("New", (_, _) => ClearForm(), 75));
        buttons.Controls.Add(UiHelper.Btn("Add", (_, _) => AddAgent(), 75));
        buttons.Controls.Add(UiHelper.Btn("Update", (_, _) => UpdateAgent(), 75));
        buttons.Controls.Add(UiHelper.Btn("Delete", (_, _) => DeleteAgent(), 75));
        buttons.Controls.Add(UiHelper.Btn("Lock / Unlock account", (_, _) => ToggleLock(), 160));

        var account = UiHelper.FieldTable();
        UiHelper.AddRow(account, "Username", txtUser);
        UiHelper.AddRow(account, "Email", txtEmail);
        UiHelper.AddRow(account, "Password", txtPass);
        chkAccount.AutoSize = true;
        chkAccount.CheckedChanged += (_, _) =>
            txtUser.Enabled = txtEmail.Enabled = txtPass.Enabled = chkAccount.Checked;

        var chkHost = new Panel { Dock = DockStyle.Top, Height = 28 };
        chkHost.Controls.Add(chkAccount);

        var fields = UiHelper.FieldTable();
        UiHelper.AddRow(fields, "Name", txtName);
        UiHelper.AddRow(fields, "Address", txtAddress);
        UiHelper.AddRow(fields, "Phone", txtPhone);

        right.Controls.Add(buttons);
        right.Controls.Add(account);
        right.Controls.Add(chkHost);
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
                "SELECT a.AgentID, a.AgentName, a.Address, a.Phone, u.UserName AS Account, u.IsLocked " +
                "FROM Agent a LEFT JOIN Users u ON u.AgentID = a.AgentID " +
                "WHERE a.AgentName LIKE @s OR a.Phone LIKE @s OR a.Address LIKE @s ORDER BY a.AgentID",
                Db.P("@s", like));
            loading = false;

            if (keep != null)
            {
                foreach (DataGridViewRow r in grid.Rows)
                {
                    if ((int)r.Cells["AgentID"].Value == keep) { grid.CurrentCell = r.Cells["AgentName"]; break; }
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
        selectedId = Convert.ToInt32(r.Cells["AgentID"].Value);
        txtName.Text = r.Cells["AgentName"].Value?.ToString() ?? "";
        txtAddress.Text = r.Cells["Address"].Value?.ToString() ?? "";
        txtPhone.Text = r.Cells["Phone"].Value?.ToString() ?? "";
    }

    private void ClearForm()
    {
        selectedId = null;
        txtName.Clear(); txtAddress.Clear(); txtPhone.Clear();
        txtUser.Clear(); txtEmail.Clear(); txtPass.Clear();
        chkAccount.Checked = false;
        grid.ClearSelection();
        txtName.Focus();
    }

    private bool ReadInput(out string name, out string address, out string phone)
    {
        name = txtName.Text.Trim();
        address = txtAddress.Text.Trim();
        phone = txtPhone.Text.Trim();
        if (name.Length == 0 || address.Length == 0)
        {
            UiHelper.Warn("Agent name and address are required.");
            return false;
        }
        if (!Regex.IsMatch(phone, @"^\d{9,11}$"))
        {
            UiHelper.Warn("Phone must be 9 to 11 digits.");
            return false;
        }
        return true;
    }

    private void AddAgent()
    {
        if (!ReadInput(out var name, out var address, out var phone)) return;

        var user = txtUser.Text.Trim();
        var email = txtEmail.Text.Trim();
        if (chkAccount.Checked)
        {
            if (user.Length == 0 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                UiHelper.Warn("Enter a username and a valid email for the portal login.");
                return;
            }
            if (txtPass.Text.Length < 6)
            {
                UiHelper.Warn("Password must be at least 6 characters.");
                return;
            }
        }

        try
        {
            using var con = Db.Open();
            using var tx = con.BeginTransaction();
            try
            {
                var id = (int)Db.Cmd(con, tx,
                    "INSERT INTO Agent (AgentName, Address, Phone) OUTPUT INSERTED.AgentID VALUES (@n, @a, @p)",
                    Db.P("@n", name), Db.P("@a", address), Db.P("@p", phone)).ExecuteScalar()!;

                if (chkAccount.Checked)
                {
                    Db.Cmd(con, tx,
                        "INSERT INTO Users (UserName, Email, Password, IsLocked, Role, AgentID) " +
                        "VALUES (@u, @e, @pw, 0, 'Agent', @id)",
                        Db.P("@u", user), Db.P("@e", email),
                        Db.P("@pw", PasswordHasher.Hash(txtPass.Text)), Db.P("@id", id)).ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }

            selectedId = null;
            LoadData();
            UiHelper.Info("Agent registered.");
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    private void UpdateAgent()
    {
        if (selectedId == null) { UiHelper.Warn("Select an agent first."); return; }
        if (!ReadInput(out var name, out var address, out var phone)) return;
        try
        {
            Db.Execute("UPDATE Agent SET AgentName=@n, Address=@a, Phone=@p WHERE AgentID=@id",
                Db.P("@n", name), Db.P("@a", address), Db.P("@p", phone), Db.P("@id", selectedId));
            LoadData();
            UiHelper.Info("Agent updated.");
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }

    private void DeleteAgent()
    {
        if (selectedId == null) { UiHelper.Warn("Select an agent first."); return; }
        if (!UiHelper.Confirm($"Delete agent '{txtName.Text}' and their portal login?")) return;
        try
        {
            using var con = Db.Open();
            using var tx = con.BeginTransaction();
            try
            {
                // Remove the login first: Users has CHECK (Role='Agent' => AgentID NOT NULL),
                // so ON DELETE SET NULL alone would be rejected.
                Db.Cmd(con, tx, "DELETE FROM Users WHERE AgentID=@id", Db.P("@id", selectedId)).ExecuteNonQuery();
                Db.Cmd(con, tx, "DELETE FROM Agent WHERE AgentID=@id", Db.P("@id", selectedId)).ExecuteNonQuery();
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }

            selectedId = null;
            LoadData();
        }
        catch (SqlException ex)
        {
            UiHelper.Error(ex.Number == 547
                ? "This agent has orders and cannot be deleted. Lock the account instead."
                : UiHelper.DbMessage(ex));
        }
    }

    private void ToggleLock()
    {
        if (selectedId == null) { UiHelper.Warn("Select an agent first."); return; }
        try
        {
            var rows = Db.Execute(
                "UPDATE Users SET IsLocked = CASE WHEN IsLocked = 1 THEN 0 ELSE 1 END WHERE AgentID=@id",
                Db.P("@id", selectedId));
            if (rows == 0) UiHelper.Warn("This agent has no portal login.");
            else LoadData();
        }
        catch (SqlException ex) { UiHelper.Error(UiHelper.DbMessage(ex)); }
    }
}
