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

                string repoPath;
                string passedPassword;
                bool startInTray;

                StartupAction action = VaultLifecycle.DetermineStartupAction(args, appSettings, out repoPath, out passedPassword, out startInTray);

                CredentialRepository repository = null;
                CredentialService service = null;

                switch (action)
                {
                    case StartupAction.OpenMainWithNoVault:
                    {
                        // Deterministic: No vault file exists on disk (start from scratch or file/settings deleted).
                        // Never show an unlock screen for a phantom file! Directly open MainForm in [No Vault Loaded] state.
                        repository = new CredentialRepository(null);
                        service = new CredentialService(repository);
                        RunMainForm(service, startInTray);
                        break;
                    }

                    case StartupAction.OpenMainWithVault:
                    {
                        // Existing unencrypted vault or successfully unlocked via CLI argument
                        repository = new CredentialRepository(repoPath);
                        service = new CredentialService(repository);

                        if (!string.IsNullOrEmpty(passedPassword))
                        {
                            service.UnlockVault(passedPassword);
                        }

                        appSettings.AddRecentVault(repoPath);
                        appSettings.Save();
                        RunMainForm(service, startInTray);
                        break;
                    }

                    case StartupAction.PromptUnlockVault:
                    {
                        // An actual encrypted .kcrypt vault exists on disk. Prompt user to unlock it.
                        using (MasterPasswordForm unlockForm = new MasterPasswordForm(MasterPasswordMode.Unlock, repoPath, inAppContext: false))
                        {
                            DialogResult res = unlockForm.ShowDialog();
                            if (res == DialogResult.OK)
                            {
                                string target = unlockForm.SelectedVaultPath;
                                repository = new CredentialRepository(target);
                                service = new CredentialService(repository);

                                if (!string.IsNullOrEmpty(unlockForm.ActiveMasterPassword))
                                {
                                    service.UnlockVault(unlockForm.ActiveMasterPassword);
                                }

                                appSettings.AddRecentVault(target);
                                appSettings.Save();
                                RunMainForm(service, startInTray);
                            }
                            else if (res == DialogResult.Abort)
                            {
                                // User explicitly chose "Exit Application"
                                return;
                            }
                            else
                            {
                                // User closed dialog (X) or cancelled -> Open MainForm in [No Vault Loaded] state!
                                // The user is NEVER trapped: they can create a new vault or open another file.
                                repository = new CredentialRepository(null);
                                service = new CredentialService(repository);
                                RunMainForm(service, startInTray);
                            }
                        }
                        break;
                    }
                }
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

        private static void RunMainForm(CredentialService service, bool startInTray)
        {
            MainForm mainForm = new MainForm(service);
            if (startInTray)
            {
                mainForm.Load += delegate { mainForm.MinimizeToTray(); };
            }
            Application.Run(mainForm);
        }
    }
}
