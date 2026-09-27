using System;
using System.Diagnostics;
using System.Threading;

namespace PasswordGui
{
    /// <summary>
    /// Manages application single-instance enforcement using a system-wide named Mutex
    /// and IPC window message broadcasting to activate existing background processes.
    /// </summary>
    public static class SingleInstanceController
    {
        public const string MutexName = @"Local\KeyCraftPasswordManager_SingleInstance_Mutex";
        public const string RestoreMessageName = "KeyCraft_RestoreWindow_Message_V1";

        public static readonly uint RestoreWindowMessageId;

        static SingleInstanceController()
        {
            RestoreWindowMessageId = NativeMethods.RegisterWindowMessage(RestoreMessageName);
            NativeMethods.AllowMessageThroughUAC(RestoreWindowMessageId);
        }

        /// <summary>
        /// Attempts to acquire the single-instance mutex.
        /// Returns true if this is the first/only running instance.
        /// </summary>
        public static bool TryAcquireMutex(out Mutex mutex)
        {
            bool createdNew;
            mutex = new Mutex(true, MutexName, out createdNew);
            return createdNew;
        }

        /// <summary>
        /// Broadcasts an IPC restore signal to any existing background instance of KeyCraft
        /// and attempts to bring its window to the foreground.
        /// </summary>
        public static void SignalRunningInstance(bool maximize = false)
        {
            IntPtr wParam = maximize ? (IntPtr)2 : (IntPtr)1;

            // 1. Primary: Broadcast the registered Windows message (reaches top-level windows even if minimized to tray)
            NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, RestoreWindowMessageId, wParam, IntPtr.Zero);

            // 2. Direct Window handle lookup via EnumWindows for target process IDs
            try
            {
                Process current = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(current.ProcessName);
                foreach (Process p in processes)
                {
                    if (p.Id != current.Id)
                    {
                        uint targetPid = (uint)p.Id;
                        NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                        {
                            uint pid;
                            NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                            if (pid == targetPid)
                            {
                                NativeMethods.PostMessage(hWnd, RestoreWindowMessageId, wParam, IntPtr.Zero);
                                NativeMethods.ShowWindowAsync(hWnd, maximize ? NativeMethods.SW_MAXIMIZE : NativeMethods.SW_RESTORE);
                                NativeMethods.SetForegroundWindow(hWnd);
                                NativeMethods.BringWindowToTop(hWnd);
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                }
            }
            catch { }
        }
    }
}
