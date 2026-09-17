using System;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Application entry point. Initializes dependencies and launches the main window.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                    System.IO.File.WriteAllText("crash.log", "UnhandledException: " + e.ExceptionObject.ToString());
                }
                catch { }
            };

            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                try
                {
                    System.IO.File.WriteAllText("crash.log", "ThreadException: " + e.Exception.ToString());
                }
                catch { }
            };

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Initialize Database / Repository layer
                CredentialRepository repository = new CredentialRepository();

                // Initialize Business / Program Logic layer
                CredentialService service = new CredentialService(repository);

                // Launch Main Layout Form
                Application.Run(new MainForm(service));
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText("crash.log", "CatchException: " + ex.ToString());
                }
                catch { }

                MessageBox.Show(
                    "A fatal error occurred: " + ex.Message,
                    "Application Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
