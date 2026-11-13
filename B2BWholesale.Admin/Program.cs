using B2BWholesale.Admin.Forms;

namespace B2BWholesale.Admin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using (var login = new LoginForm())
        {
            if (login.ShowDialog() != DialogResult.OK) return;
        }

        Application.Run(new MainForm());
    }
}
