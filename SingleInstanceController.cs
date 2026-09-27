using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Robust single-instance enforcement and IPC engine for KeyCraft.
    /// Uses a system-wide Mutex for primary detection and high-speed local Named Pipes
    /// for reliable inter-process communication without touching hidden GDI+ or .NET runtime windows.
    /// </summary>
    public static class SingleInstanceController
    {
        public const string MutexName = @"Local\KeyCraftPasswordManager_SingleInstance_Mutex";
        public const string PipeName = "KeyCraft_SingleInstance_IPC_Pipe_V2";
        public const string RestoreMessageName = "KeyCraft_RestoreWindow_Message_V1";

        public static readonly uint RestoreWindowMessageId;

        private static Thread serverThread = null;
        private static volatile bool isServerRunning = false;
        private static NamedPipeServerStream activeServerStream = null;
        private static Form registeredForm = null;
        private static readonly object syncLock = new object();

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
        /// Registers the active UI Form (e.g. MainForm or MasterPasswordForm)
        /// so incoming IPC signals can restore and focus it directly on the UI thread.
        /// </summary>
        public static void RegisterActiveForm(Form form)
        {
            lock (syncLock)
            {
                registeredForm = form;
            }
        }

        /// <summary>
        /// Unregisters the active form on close.
        /// </summary>
        public static void UnregisterActiveForm(Form form)
        {
            lock (syncLock)
            {
                if (registeredForm == form)
                {
                    registeredForm = null;
                }
            }
        }

        /// <summary>
        /// Starts the background Named Pipe IPC listener.
        /// Called by the primary instance to listen for activation requests from subsequent launches.
        /// </summary>
        public static void StartServer(Action<string> customHandler = null)
        {
            lock (syncLock)
            {
                if (isServerRunning) return;
                isServerRunning = true;

                serverThread = new Thread(delegate()
                {
                    ServerWorker(customHandler);
                });
                serverThread.IsBackground = true;
                serverThread.Name = "KeyCraft_SingleInstance_IPC_Server";
                serverThread.Start();
            }
        }

        /// <summary>
        /// Stops the background IPC listener cleanly.
        /// </summary>
        public static void StopServer()
        {
            lock (syncLock)
            {
                isServerRunning = false;
                if (activeServerStream != null)
                {
                    try
                    {
                        activeServerStream.Close();
                    }
                    catch { }
                    activeServerStream = null;
                }
            }
        }

        private static void ServerWorker(Action<string> customHandler)
        {
            while (isServerRunning)
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous
                    );

                    lock (syncLock)
                    {
                        if (!isServerRunning)
                        {
                            server.Close();
                            return;
                        }
                        activeServerStream = server;
                    }

                    // Asynchronously wait for a connection so thread can exit cleanly on StopServer
                    IAsyncResult asyncResult = server.BeginWaitForConnection(null, null);
                    while (!asyncResult.AsyncWaitHandle.WaitOne(400))
                    {
                        if (!isServerRunning)
                        {
                            server.Close();
                            return;
                        }
                    }

                    server.EndWaitForConnection(asyncResult);

                    string message = null;
                    using (StreamReader reader = new StreamReader(server, Encoding.UTF8))
                    {
                        message = reader.ReadLine();
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        if (customHandler != null)
                        {
                            customHandler(message);
                        }
                        else
                        {
                            DispatchMessage(message);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception)
                {
                    if (!isServerRunning) return;
                    Thread.Sleep(50);
                }
                finally
                {
                    if (server != null)
                    {
                        try { server.Close(); } catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Dispatches the IPC wake-up message to the registered form on the UI thread.
        /// </summary>
        public static void DispatchMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Form form = null;
            lock (syncLock)
            {
                form = registeredForm;
            }

            if (form != null && !form.IsDisposed && form.IsHandleCreated)
            {
                form.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        bool maximize = message.Contains("MAXIMIZE");

                        MainForm main = form as MainForm;
                        if (main != null)
                        {
                            main.RestoreFromTray(maximize);

                            // Optional file forwarding (e.g. RESTORE|OPEN:C:\path\to\vault.kcrypt)
                            int openIdx = message.IndexOf("OPEN:", StringComparison.OrdinalIgnoreCase);
                            if (openIdx >= 0)
                            {
                                string filePath = message.Substring(openIdx + 5).Trim();
                                if (File.Exists(filePath))
                                {
                                    main.SwitchToVaultFile(filePath);
                                }
                            }
                        }
                        else
                        {
                            // If MasterPasswordForm is active, restore and focus it
                            form.Show();
                            form.WindowState = FormWindowState.Normal;
                            form.BringToFront();
                            form.Activate();
                            NativeMethods.SetForegroundWindow(form.Handle);
                        }
                    }
                    catch { }
                }));
            }
        }

        /// <summary>
        /// Signals an existing running instance to wake up, restore its window, and bring it to front.
        /// Connects via local Named Pipe with fallback to targeted KeyCraft-only window lookup.
        /// </summary>
        public static bool SignalRunningInstance(bool maximize = false)
        {
            return SignalRunningInstance(maximize ? "RESTORE_MAXIMIZE" : "RESTORE");
        }

        /// <summary>
        /// Sends an IPC message to the running instance via Named Pipe.
        /// </summary>
        public static bool SignalRunningInstance(string message)
        {
            if (string.IsNullOrEmpty(message)) message = "RESTORE";

            // 1. Primary: High-speed local Named Pipe connection (<2ms)
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(800); // 800ms timeout
                    using (StreamWriter writer = new StreamWriter(client, Encoding.UTF8))
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                    }
                    return true;
                }
            }
            catch
            {
                // 2. Secondary Fallback: Targeted Win32 window search that EXCLUSIVELY targets KeyCraft windows
                // and NEVER touches GDI+ or .NET runtime broadcast helper windows!
                return FallbackTargetedWin32Signal(message);
            }
        }

        /// <summary>
        /// Ultra-safe Win32 fallback that specifically filters for windows titled 'KeyCraft'
        /// and strictly ignores all internal runtime windows (GDI+, .NET-BroadcastEventWindow, etc.).
        /// </summary>
        private static bool FallbackTargetedWin32Signal(string message)
        {
            bool foundAndSignaled = false;
            try
            {
                bool maximize = message != null && message.Contains("MAXIMIZE");
                IntPtr wParam = maximize ? (IntPtr)2 : (IntPtr)1;

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
                                int len = NativeMethods.GetWindowTextLength(hWnd);
                                if (len > 0)
                                {
                                    StringBuilder sb = new StringBuilder(len + 1);
                                    NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                                    string title = sb.ToString();

                                    // CRITICAL FILTER: ONLY touch legitimate KeyCraft user interface windows!
                                    // Strictly reject "GDI+ Window", ".NET-BroadcastEventWindow", and blank helper handles.
                                    if (title.StartsWith("KeyCraft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        NativeMethods.PostMessage(hWnd, RestoreWindowMessageId, wParam, IntPtr.Zero);
                                        NativeMethods.ShowWindowAsync(hWnd, maximize ? NativeMethods.SW_MAXIMIZE : NativeMethods.SW_RESTORE);
                                        NativeMethods.SetForegroundWindow(hWnd);
                                        NativeMethods.BringWindowToTop(hWnd);
                                        foundAndSignaled = true;
                                        return false; // Found legitimate window, stop enumeration!
                                    }
                                }
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (foundAndSignaled) break;
                    }
                }
            }
            catch { }

            return foundAndSignaled;
        }
    }
}
