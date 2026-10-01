using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasswordGui
{
    /// <summary>
    /// Persistent application preferences and Windows startup manager.
    /// Stores user settings in data/settings.conf and configures Windows registry auto-start.
    /// </summary>
    public class AppSettings
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "KeyCraftPasswordManager";
        private static readonly string SettingsFilePath = GetSettingsFilePath();

        public bool RunOnStartup { get; set; }
        public bool CloseToTray { get; set; }
        public bool HotkeyEnabled { get; set; }
        public int HotkeyModifiers { get; set; } // MOD_CONTROL(2), MOD_ALT(1), MOD_SHIFT(4), MOD_WIN(8)
        public Keys HotkeyKey { get; set; }
        public string LastOpenedVaultPath { get; set; }
        public string LastFilterBy { get; set; }
        public System.Collections.Generic.List<string> RecentVaults { get; set; }
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool WindowMaximized { get; set; }

        public AppSettings()
        {
            // Defaults as specified by user
            RunOnStartup = false;
            CloseToTray = false; // Default: X closes the program, not minimize to tray
            HotkeyEnabled = true;
            HotkeyModifiers = 0x0001 | 0x0002; // Alt + Control
            HotkeyKey = Keys.K;
            RecentVaults = new System.Collections.Generic.List<string>();
            LastOpenedVaultPath = null;
            LastFilterBy = "Filter by: SlNo";
            WindowX = -1;
            WindowY = -1;
            WindowWidth = 1280;
            WindowHeight = 820;
            WindowMaximized = false;
        }

        public static string GetKeyCraftDirectory()
        {
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userHome))
            {
                userHome = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            string keyCraftDir = Path.Combine(userHome, "KeyCraft");
            if (!Directory.Exists(keyCraftDir))
            {
                try { Directory.CreateDirectory(keyCraftDir); } catch { }
            }
            return keyCraftDir;
        }

        public static string GetSettingsFilePath()
        {
            return Path.Combine(GetKeyCraftDirectory(), "settings.conf");
        }

        public static string GetDefaultVaultPath()
        {
            return Path.Combine(GetKeyCraftDirectory(), "vault.kcrypt");
        }

        public void AddRecentVault(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (RecentVaults == null) RecentVaults = new System.Collections.Generic.List<string>();

                RecentVaults.RemoveAll(p => string.Equals(p, fullPath, StringComparison.OrdinalIgnoreCase));
                RecentVaults.Insert(0, fullPath);

                if (RecentVaults.Count > 10)
                {
                    RecentVaults.RemoveRange(10, RecentVaults.Count - 10);
                }

                LastOpenedVaultPath = fullPath;
            }
            catch { }
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();

            try
            {
                string filePath = GetSettingsFilePath();
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    foreach (string rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                        int eqIdx = line.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            string key = line.Substring(0, eqIdx).Trim();
                            string val = line.Substring(eqIdx + 1).Trim();

                            if (string.Equals(key, "RunOnStartup", StringComparison.OrdinalIgnoreCase))
                            {
                                bool b; if (bool.TryParse(val, out b)) settings.RunOnStartup = b;
                            }
                            else if (string.Equals(key, "CloseToTray", StringComparison.OrdinalIgnoreCase))
                            {
                                bool b; if (bool.TryParse(val, out b)) settings.CloseToTray = b;
                            }
                            else if (string.Equals(key, "HotkeyEnabled", StringComparison.OrdinalIgnoreCase))
                            {
                                bool b; if (bool.TryParse(val, out b)) settings.HotkeyEnabled = b;
                            }
                            else if (string.Equals(key, "HotkeyModifiers", StringComparison.OrdinalIgnoreCase))
                            {
                                int m; if (int.TryParse(val, out m)) settings.HotkeyModifiers = m;
                            }
                            else if (string.Equals(key, "HotkeyKey", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    settings.HotkeyKey = (Keys)Enum.Parse(typeof(Keys), val, true);
                                }
                                catch { }
                            }
                            else if (string.Equals(key, "LastOpenedVaultPath", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(val)) settings.LastOpenedVaultPath = val;
                            }
                            else if (string.Equals(key, "LastFilterBy", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(val)) settings.LastFilterBy = val;
                            }
                            else if (string.Equals(key, "WindowX", StringComparison.OrdinalIgnoreCase))
                            {
                                int x; if (int.TryParse(val, out x)) settings.WindowX = x;
                            }
                            else if (string.Equals(key, "WindowY", StringComparison.OrdinalIgnoreCase))
                            {
                                int y; if (int.TryParse(val, out y)) settings.WindowY = y;
                            }
                            else if (string.Equals(key, "WindowWidth", StringComparison.OrdinalIgnoreCase))
                            {
                                int w; if (int.TryParse(val, out w)) settings.WindowWidth = w;
                            }
                            else if (string.Equals(key, "WindowHeight", StringComparison.OrdinalIgnoreCase))
                            {
                                int h; if (int.TryParse(val, out h)) settings.WindowHeight = h;
                            }
                            else if (string.Equals(key, "WindowMaximized", StringComparison.OrdinalIgnoreCase))
                            {
                                bool m; if (bool.TryParse(val, out m)) settings.WindowMaximized = m;
                            }
                            else if (string.Equals(key, "RecentVaults", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(val))
                                {
                                    string[] parts = val.Split(';');
                                    foreach (string p in parts)
                                    {
                                        string trimmedP = p.Trim();
                                        if (!string.IsNullOrEmpty(trimmedP) && !settings.RecentVaults.Contains(trimmedP))
                                        {
                                            settings.RecentVaults.Add(trimmedP);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // Ensure last opened vault path is valid or default
            if (string.IsNullOrEmpty(settings.LastOpenedVaultPath))
            {
                settings.LastOpenedVaultPath = GetDefaultVaultPath();
            }

            if (!settings.RecentVaults.Contains(settings.LastOpenedVaultPath) && File.Exists(settings.LastOpenedVaultPath))
            {
                settings.RecentVaults.Insert(0, settings.LastOpenedVaultPath);
            }

            return settings;
        }

        public void Save()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# KeyCraft Configuration File");
                sb.AppendLine("RunOnStartup=" + RunOnStartup);
                sb.AppendLine("CloseToTray=" + CloseToTray);
                sb.AppendLine("HotkeyEnabled=" + HotkeyEnabled);
                sb.AppendLine("HotkeyModifiers=" + HotkeyModifiers);
                sb.AppendLine("HotkeyKey=" + HotkeyKey);
                sb.AppendLine("LastOpenedVaultPath=" + (LastOpenedVaultPath ?? string.Empty));
                sb.AppendLine("WindowX=" + WindowX);
                sb.AppendLine("WindowY=" + WindowY);
                sb.AppendLine("WindowWidth=" + WindowWidth);
                sb.AppendLine("WindowHeight=" + WindowHeight);
                sb.AppendLine("WindowMaximized=" + WindowMaximized);
                sb.AppendLine("LastFilterBy=" + (LastFilterBy ?? "Filter by: SlNo"));
                if (RecentVaults != null && RecentVaults.Count > 0)
                {
                    sb.AppendLine("RecentVaults=" + string.Join(";", RecentVaults.ToArray()));
                }

                SafeFileStorage.WriteAllTextAtomic(filePath, sb.ToString(), false);

                // Apply to Windows Registry
                ApplyStartupRegistry(RunOnStartup);
            }
            catch { }
        }

        /// <summary>
        /// Registers or unregisters KeyCraft in the current user's Windows Run registry key.
        /// </summary>
        public static void ApplyStartupRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Application.ExecutablePath;
                            string command = "\"" + exePath + "\" --tray";
                            key.SetValue(AppRegistryName, command);
                        }
                        else
                        {
                            key.DeleteValue(AppRegistryName, false);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
