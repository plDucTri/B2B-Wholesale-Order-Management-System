using B2BWholesale.Admin.Data;
using B2BWholesale.Admin.Security;

namespace B2BWholesale.Admin.Forms;

/// <summary>Task 1.1 - only unlocked users with Role = 'Admin' may enter.</summary>
public class LoginForm : Form
{
    private readonly TextBox txtUser = new();
    private readonly TextBox txtPass = new() { UseSystemPasswordChar = true };
    private int attempts;

    public LoginForm()
    {
        Text = "B2B Wholesale - Admin Login";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(360, 190);

        var title = new Label
        {
            Text = "Warehouse Back-Office",
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 45,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var table = UiHelper.FieldTable();
        table.Padding = new Padding(15, 5, 15, 5);
        UiHelper.AddRow(table, "Username", txtUser);
        UiHelper.AddRow(table, "Password", txtPass);

        var btnLogin = new Button { Text = "Login", Width = 100, Height = 32, Left = 130, Top = 140 };
        btnLogin.Click += (_, _) => DoLogin();
        AcceptButton = btnLogin;

        Controls.Add(btnLogin);
        Controls.Add(table);
        Controls.Add(title);
    }

    private void DoLogin()
    {
        var user = txtUser.Text.Trim();
        var pass = txtPass.Text;
        if (user.Length == 0 || pass.Length == 0)
        {
            UiHelper.Warn("Please enter username and password.");
            return;
        }

        try
        {
            var dt = Db.Query(
                "SELECT UserID, UserName, Password, IsLocked, Role FROM Users WHERE UserName = @u",
                Db.P("@u", user));

            // Same generic message for unknown user / wrong password (no user enumeration).
            if (dt.Rows.Count == 0 || !PasswordHasher.Verify(pass, (string)dt.Rows[0]["Password"]))
            {
                Fail("Invalid username or password.");
                return;
            }

            var row = dt.Rows[0];
            if ((bool)row["IsLocked"]) { Fail("This account is locked. Contact the system administrator."); return; }
            if (!string.Equals((string)row["Role"], "Admin", StringComparison.OrdinalIgnoreCase))
            {
                Fail("Access denied: this application is for Admin users only.");
                return;
            }

            Session.UserId = (int)row["UserID"];
            Session.UserName = (string)row["UserName"];
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            UiHelper.Error("Cannot connect to the database. Check App.config.\n\n" + ex.Message);
        }
    }

    private void Fail(string message)
    {
        UiHelper.Warn(message);
        txtPass.Clear();
        txtPass.Focus();
        if (++attempts >= 3)
        {
            UiHelper.Error("Too many failed attempts. The application will close.");
            DialogResult = DialogResult.Cancel;
        }
    }
}