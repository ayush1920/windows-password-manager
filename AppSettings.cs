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
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "settings.conf");

        public bool RunOnStartup { get; set; }
        public bool CloseToTray { get; set; }
        public bool HotkeyEnabled { get; set; }
        public int HotkeyModifiers { get; set; } // MOD_CONTROL(2), MOD_ALT(1), MOD_SHIFT(4), MOD_WIN(8)
        public Keys HotkeyKey { get; set; }

        public AppSettings()
        {
            // Defaults as specified by user
            RunOnStartup = false;
            CloseToTray = false; // Default: X closes the program, not minimize to tray
            HotkeyEnabled = true;
            HotkeyModifiers = 0x0001 | 0x0002; // Alt + Control
            HotkeyKey = Keys.K;
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string[] lines = File.ReadAllLines(SettingsFilePath, Encoding.UTF8);
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
                        }
                    }
                }
            }
            catch { }

            return settings;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# KeyCraft Configuration File");
                sb.AppendLine("RunOnStartup=" + RunOnStartup);
                sb.AppendLine("CloseToTray=" + CloseToTray);
                sb.AppendLine("HotkeyEnabled=" + HotkeyEnabled);
                sb.AppendLine("HotkeyModifiers=" + HotkeyModifiers);
                sb.AppendLine("HotkeyKey=" + HotkeyKey);

                SafeFileStorage.WriteAllTextAtomic(SettingsFilePath, sb.ToString(), false);

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
