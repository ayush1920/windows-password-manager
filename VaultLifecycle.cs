using System;
using System.IO;

namespace PasswordGui
{
    /// <summary>
    /// Deterministic startup decision actions for application lifecycle.
    /// </summary>
    public enum StartupAction
    {
        /// <summary>
        /// No existing vault file exists on disk (starting from scratch or file/settings deleted).
        /// Directly open the Main Application Window in [No Vault Loaded] state. Never show an unlock screen!
        /// </summary>
        OpenMainWithNoVault,

        /// <summary>
        /// A valid plaintext or pre-authenticated vault exists on disk. Open MainForm directly with active vault.
        /// </summary>
        OpenMainWithVault,

        /// <summary>
        /// An encrypted .kcrypt vault exists on disk. Prompt user to unlock it.
        /// </summary>
        PromptUnlockVault
    }

    /// <summary>
    /// Senior architectural controller for deterministic vault path resolution and startup state management.
    /// Guarantees that the application never requests an unlock password for a non-existent or deleted file.
    /// Follows the DRY principle and separates lifecycle state resolution from UI rendering.
    /// </summary>
    public static class VaultLifecycle
    {
        /// <summary>
        /// Deterministically resolves the existing vault file on disk and decides startup flow.
        /// Guaranteed: If no vault file exists on disk, targetPath is null and action is OpenMainWithNoVault.
        /// </summary>
        public static StartupAction DetermineStartupAction(
            string[] args,
            AppSettings settings,
            out string resolvedVaultPath,
            out string cliPassword,
            out bool startInTray)
        {
            resolvedVaultPath = null;
            cliPassword = null;
            startInTray = false;

            string explicitCliFile = null;

            // 1. Parse Command-Line Arguments
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i];
                    if ((a.Equals("-p", StringComparison.OrdinalIgnoreCase) ||
                         a.Equals("--password", StringComparison.OrdinalIgnoreCase) ||
                         a.Equals("--master-pass", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                    {
                        cliPassword = args[i + 1];
                        i++;
                    }
                    else if (a.Equals("--tray", StringComparison.OrdinalIgnoreCase) ||
                             a.Equals("--min", StringComparison.OrdinalIgnoreCase))
                    {
                        startInTray = true;
                    }
                    else if ((a.Equals("-f", StringComparison.OrdinalIgnoreCase) ||
                              a.Equals("--file", StringComparison.OrdinalIgnoreCase) ||
                              a.Equals("--vault", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                    {
                        explicitCliFile = args[i + 1];
                        i++;
                    }
                    else if (!a.StartsWith("-") && (a.EndsWith(".kcrypt", StringComparison.OrdinalIgnoreCase) ||
                                                    a.EndsWith(".kdb", StringComparison.OrdinalIgnoreCase) ||
                                                    a.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                    {
                        explicitCliFile = a;
                    }
                }
            }

            // 2. Deterministically resolve existing file on disk
            if (!string.IsNullOrEmpty(explicitCliFile))
            {
                if (File.Exists(explicitCliFile))
                {
                    resolvedVaultPath = Path.GetFullPath(explicitCliFile);
                }
                else
                {
                    // Explicit file does not exist -> do not guess!
                    resolvedVaultPath = null;
                }
            }
            else if (settings != null && !string.IsNullOrEmpty(settings.LastOpenedVaultPath) && File.Exists(settings.LastOpenedVaultPath))
            {
                resolvedVaultPath = Path.GetFullPath(settings.LastOpenedVaultPath);
            }
            else
            {
                string defaultPath = AppSettings.GetDefaultVaultPath();
                if (File.Exists(defaultPath))
                {
                    resolvedVaultPath = Path.GetFullPath(defaultPath);
                }
                else
                {
                    // No vault file exists anywhere on disk!
                    resolvedVaultPath = null;
                }
            }

            // 3. Deterministic Startup Flow Decision
            if (string.IsNullOrEmpty(resolvedVaultPath))
            {
                // NO FILE EXISTS ON DISK -> Open MainForm directly in [No Vault Loaded] state!
                return StartupAction.OpenMainWithNoVault;
            }

            // A file DOES exist on disk. Check encryption.
            bool isEncrypted = VaultSecurity.IsVaultEncrypted(resolvedVaultPath);

            if (!isEncrypted)
            {
                // Plaintext legacy file -> Open directly
                return StartupAction.OpenMainWithVault;
            }

            // Encrypted file (.kcrypt):
            if (!string.IsNullOrEmpty(cliPassword))
            {
                string dummy;
                if (VaultSecurity.UnlockVault(resolvedVaultPath, cliPassword, out dummy))
                {
                    return StartupAction.OpenMainWithVault;
                }
            }

            // Prompt user to unlock this existing file
            return StartupAction.PromptUnlockVault;
        }
    }
}
