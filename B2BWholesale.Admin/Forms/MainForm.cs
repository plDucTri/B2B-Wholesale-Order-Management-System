using B2BWholesale.Admin.Data;

namespace B2BWholesale.Admin.Forms;

public class MainForm : Form
{
    public MainForm()
    {
        Text = $"B2B Wholesale Management - Back Office  (user: {Session.UserName})";
        WindowState = FormWindowState.Maximized;
        IsMdiContainer = true;

        var menu = new MenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Items", null, (_, _) => OpenChild<ItemsForm>()));
        menu.Items.Add(new ToolStripMenuItem("Agents", null, (_, _) => OpenChild<AgentsForm>()));
        menu.Items.Add(new ToolStripMenuItem("Orders", null, (_, _) => OpenChild<OrdersForm>()));
        menu.Items.Add(new ToolStripMenuItem("Dashboard", null, (_, _) => OpenChild<DashboardForm>()));
        menu.Items.Add(new ToolStripMenuItem("Logout", null, (_, _) =>
        {
            if (UiHelper.Confirm("Log out and return to the login screen?")) Application.Restart();
        }));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Close()));

        MainMenuStrip = menu;
        Controls.Add(menu);

        Shown += (_, _) => OpenChild<DashboardForm>();
    }

    private void OpenChild<T>() where T : Form, new()
    {
        foreach (var f in MdiChildren)
        {
            if (f is T) { f.Activate(); return; }
        }
        var child = new T { MdiParent = this };
        child.Show();
        child.WindowState = FormWindowState.Maximized;
    }
}
