using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Native Windows Global Hotkey Engine.
    /// Manages system-wide hotkey registration, conflict detection via Win32 API,
    /// and hotkey message routing.
    /// </summary>
    public class GlobalHotkeyManager : IDisposable
    {
        public const int WM_HOTKEY = 0x0312;
        public const int DefaultHotkeyId = 0x9001;
        private const int TestHotkeyId = 0x9002;

        // Modifiers
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
        public const int MOD_NOREPEAT = 0x4000;

        private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr boundHwnd = IntPtr.Zero;
        private bool isRegistered = false;
        private int currentModifiers = 0;
        private Keys currentKey = Keys.None;

        public event EventHandler HotkeyPressed;

        public bool IsRegistered
        {
            get { return isRegistered; }
        }

        public int CurrentModifiers
        {
            get { return currentModifiers; }
        }

        public Keys CurrentKey
        {
            get { return currentKey; }
        }

        /// <summary>
        /// Tests whether a given hotkey combination is available or already registered by another application.
        /// </summary>
        public static bool TestHotkeyAvailability(IntPtr hWnd, int modifiers, Keys key, out string warning)
        {
            warning = null;
            if (key == Keys.None)
            {
                warning = "Please select a valid key.";
                return false;
            }

            // Attempt temporary registration
            uint mods = (uint)(modifiers | MOD_NOREPEAT);
            bool success = RegisterHotKey(hWnd, TestHotkeyId, mods, (uint)key);

            if (!success)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                    warning = "⚠ Hotkey is already in use by Windows or another application.";
                }
                else
                {
                    warning = "⚠ Unable to register hotkey (Win32 Error: " + err + ").";
                }
                return false;
            }

            // Immediately release test registration
            UnregisterHotKey(hWnd, TestHotkeyId);
            return true;
        }

        /// <summary>
        /// Registers a global hotkey on the specified window handle.
        /// </summary>
        public bool Register(IntPtr hWnd, int modifiers, Keys key, out string error)
        {
            error = null;
            Unregister();

            if (hWnd == IntPtr.Zero)
            {
                error = "Window handle is invalid.";
                return false;
            }

            if (key == Keys.None)
            {
                error = "A primary key must be selected.";
                return false;
            }

            uint mods = (uint)(modifiers | MOD_NOREPEAT);
            bool success = RegisterHotKey(hWnd, DefaultHotkeyId, mods, (uint)key);

            if (!success)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                    error = "Hotkey is already mapped by another process or Windows.";
                }
                else
                {
                    error = "Failed to register hotkey (Win32 Error: " + err + ").";
                }
                return false;
            }

            this.boundHwnd = hWnd;
            this.currentModifiers = modifiers;
            this.currentKey = key;
            this.isRegistered = true;
            return true;
        }

        /// <summary>
        /// Unregisters the current active global hotkey.
        /// </summary>
        public void Unregister()
        {
            if (isRegistered && boundHwnd != IntPtr.Zero)
            {
                try
                {
                    UnregisterHotKey(boundHwnd, DefaultHotkeyId);
                }
                catch { }
            }
            isRegistered = false;
        }

        /// <summary>
        /// Processes window messages to intercept WM_HOTKEY.
        /// </summary>
        public bool ProcessMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == DefaultHotkeyId)
            {
                if (HotkeyPressed != null)
                {
                    HotkeyPressed(this, EventArgs.Empty);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Formats modifiers and key into a clean string representation (e.g. "Ctrl + Alt + K").
        /// </summary>
        public static string FormatHotkey(int modifiers, Keys key)
        {
            if (key == Keys.None && modifiers == 0) return "None";

            StringBuilder sb = new StringBuilder();
            if ((modifiers & MOD_WIN) != 0) sb.Append("Win + ");
            if ((modifiers & MOD_CONTROL) != 0) sb.Append("Ctrl + ");
            if ((modifiers & MOD_ALT) != 0) sb.Append("Alt + ");
            if ((modifiers & MOD_SHIFT) != 0) sb.Append("Shift + ");

            if (key != Keys.None)
            {
                sb.Append(FormatKeyName(key));
            }
            else if (sb.Length > 3)
            {
                sb.Length -= 3; // Trim trailing " + "
            }

            return sb.ToString();
        }

        public static string FormatKeyName(Keys key)
        {
            switch (key)
            {
                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.OemQuestion: return "/";
                case Keys.OemSemicolon: return ";";
                case Keys.OemQuotes: return "'";
                case Keys.Oemtilde: return "`";
                case Keys.OemOpenBrackets: return "[";
                case Keys.OemCloseBrackets: return "]";
                case Keys.OemPipe: return "\\";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "=";
                case Keys.D0: return "0";
                case Keys.D1: return "1";
                case Keys.D2: return "2";
                case Keys.D3: return "3";
                case Keys.D4: return "4";
                case Keys.D5: return "5";
                case Keys.D6: return "6";
                case Keys.D7: return "7";
                case Keys.D8: return "8";
                case Keys.D9: return "9";
                default: return key.ToString();
            }
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
