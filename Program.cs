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
        static void Main(string[] args)
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

            System.Threading.Mutex singleInstanceMutex = null;
            try
            {
                if (!SingleInstanceController.TryAcquireMutex(out singleInstanceMutex))
                {
                    // An instance is already running! Signal it to wake/restore and exit this process
                    bool maximize = false;
                    if (args != null && args.Length > 0)
                    {
                        foreach (string arg in args)
                        {
                            if (arg.Equals("--max", StringComparison.OrdinalIgnoreCase) ||
                                arg.Equals("--maximize", StringComparison.OrdinalIgnoreCase))
                            {
                                maximize = true;
                                break;
                            }
                        }
                    }
                    SingleInstanceController.SignalRunningInstance(maximize);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string repoPath = CredentialRepository.GetDefaultPath();
                bool isEncrypted = VaultSecurity.IsVaultEncrypted(repoPath);

                string passedPassword = null;
                bool startInTray = false;
                if (args != null && args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if ((args[i].Equals("-p", StringComparison.OrdinalIgnoreCase) ||
                             args[i].Equals("--password", StringComparison.OrdinalIgnoreCase) ||
                             args[i].Equals("--master-pass", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                        {
                            passedPassword = args[i + 1];
                            i++;
                        }
                        else if (args[i].Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                                 args[i].Equals("--min", StringComparison.OrdinalIgnoreCase))
                        {
                            startInTray = true;
                        }
                    }
                }

                bool unlockedViaCli = false;
                if (!string.IsNullOrEmpty(passedPassword) && isEncrypted)
                {
                    string decrypted;
                    if (VaultSecurity.UnlockVault(repoPath, passedPassword, out decrypted))
                    {
                        unlockedViaCli = true;
                    }
                }

                if (!unlockedViaCli)
                {
                    if (!isEncrypted)
                    {
                        // First run / unencrypted vault: user must create master password
                        using (MasterPasswordForm createForm = new MasterPasswordForm(MasterPasswordMode.Create, repoPath))
                        {
                            if (createForm.ShowDialog() != DialogResult.OK)
                            {
                                return; // Cancelled
                            }
                        }
                    }
                    else
                    {
                        // Encrypted vault: user must unlock with master password
                        using (MasterPasswordForm unlockForm = new MasterPasswordForm(MasterPasswordMode.Unlock, repoPath))
                        {
                            if (unlockForm.ShowDialog() != DialogResult.OK)
                            {
                                return; // Cancelled
                            }
                        }
                    }
                }

                // Initialize Database / Repository layer with unlocked vault
                CredentialRepository repository = new CredentialRepository(repoPath);

                // Initialize Business / Program Logic layer
                CredentialService service = new CredentialService(repository);

                // Launch Main Layout Form
                MainForm mainForm = new MainForm(service);
                if (startInTray)
                {
                    mainForm.Load += delegate { mainForm.MinimizeToTray(); };
                }
                Application.Run(mainForm);
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
            finally
            {
                if (singleInstanceMutex != null)
                {
                    try
                    {
                        singleInstanceMutex.ReleaseMutex();
                        singleInstanceMutex.Close();
                    }
                    catch { }
                }
            }
        }
    }
}
