using System;
using System.Windows.Forms;

namespace DriveSync.UI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var settings = DriveSync.Shared.SettingsStore.Load();
            Application.Run(new MainForm(settings));
        }
    }
}
