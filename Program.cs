using VetMS.Data;
using VetMS.Forms;

namespace VetMS;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            Database.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to connect to the database:\n\n{ex.Message}\n\nCheck appsettings.json and make sure PostgreSQL is running.",
                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm());
    }
}
