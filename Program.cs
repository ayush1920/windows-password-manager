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
                    string fileToOpen = null;
                    if (args != null && args.Length > 0)
                    {
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (args[i].Equals("--max", StringComparison.OrdinalIgnoreCase) ||
                                args[i].Equals("--maximize", StringComparison.OrdinalIgnoreCase))
                            {
                                maximize = true;
                            }
                            else if ((args[i].Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                                      args[i].Equals("--file", StringComparison.OrdinalIgnoreCase) ||
                                      args[i].Equals("--vault", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                            {
                                fileToOpen = args[i + 1];
                                i++;
                            }
                            else if (!args[i].StartsWith("-") && (args[i].EndsWith(".kcrypt", StringComparison.OrdinalIgnoreCase) || args[i].EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                            {
                                fileToOpen = args[i];
                            }
                        }
                    }

                    string signalMsg = maximize ? "RESTORE_MAXIMIZE" : "RESTORE";
                    if (!string.IsNullOrEmpty(fileToOpen))
                    {
                        signalMsg += "|OPEN:" + fileToOpen;
                    }

                    SingleInstanceController.SignalRunningInstance(signalMsg);
                    return;
                }

                // Primary instance: start Named Pipe IPC server
                SingleInstanceController.StartServer();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppSettings appSettings = AppSettings.Load();

                string repoPath = null;
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
                        else if ((args[i].Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                                  args[i].Equals("--file", StringComparison.OrdinalIgnoreCase) ||
                                  args[i].Equals("--vault", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                        {
                            repoPath = args[i + 1];
                            i++;
                        }
                        else if (!args[i].StartsWith("-") && System.IO.File.Exists(args[i]))
                        {
                            repoPath = args[i];
                        }
                    }
                }

                if (string.IsNullOrEmpty(repoPath))
                {
                    if (!string.IsNullOrEmpty(appSettings.LastOpenedVaultPath) && (System.IO.File.Exists(appSettings.LastOpenedVaultPath) || !System.IO.File.Exists(AppSettings.GetDefaultVaultPath())))
                    {
                        repoPath = appSettings.LastOpenedVaultPath;
                    }
                    else
                    {
                        repoPath = AppSettings.GetDefaultVaultPath();
                    }
                }

                bool isEncrypted = VaultSecurity.IsVaultEncrypted(repoPath);

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
                    MasterPasswordMode initMode = isEncrypted ? MasterPasswordMode.Unlock : MasterPasswordMode.Create;
                    using (MasterPasswordForm authForm = new MasterPasswordForm(initMode, repoPath))
                    {
                        if (authForm.ShowDialog() != DialogResult.OK)
                        {
                            return; // Cancelled
                        }
                        repoPath = authForm.SelectedVaultPath;
                    }
                }

                appSettings.AddRecentVault(repoPath);
                appSettings.Save();

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
                SingleInstanceController.StopServer();
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
