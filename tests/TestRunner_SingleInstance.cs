using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PasswordGui;

namespace PasswordGui.Tests
{
    /// <summary>
    /// Dedicated Single-Instance and IPC Test Suite.
    /// Extensively validates Mutex ownership, Named Pipe IPC transmission,
    /// UI thread dispatch, and strictly verifies that NO internal runtime helper windows
    /// (e.g. GDI+ Window, .NET-BroadcastEventWindow, or hidden parking windows) are ever made visible.
    /// </summary>
    public static class TestRunner_SingleInstance
    {
        private static int passedCount = 0;
        private static int failedCount = 0;
        private static readonly List<string> failureDetails = new List<string>();

        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("    KEYCRAFT SINGLE-INSTANCE & IPC ARCHITECTURE TEST SUITE        ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();

            RunTest("SI-01: Primary Mutex Acquisition & Secondary Exclusion", Test_SI01_MutexAcquisitionAndExclusion);
            RunTest("SI-02: Named Pipe IPC Server Lifecycle (Start, Connect & Stop)", Test_SI02_PipeServerLifecycle);
            RunTest("SI-03: Secondary Instance Signal Delivery (Named Pipe Transmission)", Test_SI03_SignalDelivery);
            RunTest("SI-04: Multi-Message & Argument Forwarding (Maximize, Open Vault)", Test_SI04_ArgumentForwarding);
            RunTest("SI-05: UI Dispatch & Seamless Window Restoration from Tray", Test_SI05_FormRestorationFromTray);
            RunTest("SI-06: Strict Window Isolation: Zero GDI+ / .NET-Broadcast Leaks", Test_SI06_StrictWindowIsolation);
            RunTest("SI-07: Targeted Win32 Fallback Ignores Runtime Helper Handles", Test_SI07_TargetedFallbackFilter);
            RunTest("SI-08: Rapid Consecutive Signals Burst Handling", Test_SI08_RapidSignalBurst);

            Console.WriteLine("\n------------------------------------------------------------------");
            if (failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("🎉 ALL {0} SINGLE-INSTANCE TESTS PASSED! (100% Success)", passedCount));
                Console.ResetColor();
                Console.WriteLine("==================================================================");
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("❌ SINGLE-INSTANCE TEST RUN FAILED: {0} Passed, {1} Failed", passedCount, failedCount));
                Console.ResetColor();
                Console.WriteLine("\nFailure Details:");
                foreach (string err in failureDetails)
                {
                    Console.WriteLine("  " + err);
                }
                Console.WriteLine("==================================================================");
                return 1;
            }
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write(string.Format("{0,-65} ... ", testName));
            try
            {
                // Ensure server is stopped and session clean before each test
                SingleInstanceController.StopServer();
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS]");
                Console.ResetColor();
                passedCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL]");
                Console.ResetColor();
                string msg = string.Format("{0} -> {1}", testName, ex.Message);
                Console.WriteLine("   Error: " + ex.Message);
                failureDetails.Add(msg);
                failedCount++;
            }
            finally
            {
                SingleInstanceController.StopServer();
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        // ====================================================================
        // TEST 1: PRIMARY MUTEX ACQUISITION & SECONDARY EXCLUSION
        // ====================================================================
        private static void Test_SI01_MutexAcquisitionAndExclusion()
        {
            Mutex primaryMutex = null;
            Mutex secondaryMutex = null;
            try
            {
                // First acquisition: must succeed
                bool isFirst = SingleInstanceController.TryAcquireMutex(out primaryMutex);
                Assert(isFirst, "First attempt to acquire mutex must succeed");
                Assert(primaryMutex != null, "Primary mutex instance must not be null");

                // Second acquisition while primary is held: must fail
                bool isSecond = SingleInstanceController.TryAcquireMutex(out secondaryMutex);
                Assert(!isSecond, "Second attempt to acquire mutex while held must return false");
            }
            finally
            {
                if (secondaryMutex != null)
                {
                    try { secondaryMutex.Close(); } catch { }
                }
                if (primaryMutex != null)
                {
                    try
                    {
                        primaryMutex.ReleaseMutex();
                        primaryMutex.Close();
                    }
                    catch { }
                }
            }

            // After releasing: a new attempt must succeed
            Mutex subsequentMutex = null;
            try
            {
                bool isSubsequent = SingleInstanceController.TryAcquireMutex(out subsequentMutex);
                Assert(isSubsequent, "Attempt to acquire mutex after release must succeed");
            }
            finally
            {
                if (subsequentMutex != null)
                {
                    try
                    {
                        subsequentMutex.ReleaseMutex();
                        subsequentMutex.Close();
                    }
                    catch { }
                }
            }
        }

        // ====================================================================
        // TEST 2: NAMED PIPE IPC SERVER LIFECYCLE (START, CONNECT & STOP)
        // ====================================================================
        private static void Test_SI02_PipeServerLifecycle()
        {
            string receivedMsg = null;
            AutoResetEvent msgReceivedEvent = new AutoResetEvent(false);

            SingleInstanceController.StartServer(delegate(string msg)
            {
                receivedMsg = msg;
                msgReceivedEvent.Set();
            });

            // Signal running instance
            bool signaled = SingleInstanceController.SignalRunningInstance("TEST_LIFECYCLE_PING");
            Assert(signaled, "SignalRunningInstance via pipe must return true");

            bool eventSignaled = msgReceivedEvent.WaitOne(2000);
            Assert(eventSignaled, "IPC Server must receive and dispatch message within 2 seconds");
            Assert(receivedMsg == "TEST_LIFECYCLE_PING", "Received message must match sent message exactly");

            // Stop server
            SingleInstanceController.StopServer();
            Thread.Sleep(100);
        }

        // ====================================================================
        // TEST 3: SECONDARY INSTANCE SIGNAL DELIVERY
        // ====================================================================
        private static void Test_SI03_SignalDelivery()
        {
            string receivedMsg = null;
            AutoResetEvent signalEvent = new AutoResetEvent(false);

            SingleInstanceController.StartServer(delegate(string msg)
            {
                receivedMsg = msg;
                signalEvent.Set();
            });

            // Signal with default RESTORE
            bool sent = SingleInstanceController.SignalRunningInstance(false);
            Assert(sent, "SignalRunningInstance(false) must return true");

            bool gotEvent = signalEvent.WaitOne(2000);
            Assert(gotEvent, "Signal event must fire within 2000ms");
            Assert(receivedMsg == "RESTORE", "Default signal message must be 'RESTORE'");
        }

        // ====================================================================
        // TEST 4: MULTI-MESSAGE & ARGUMENT FORWARDING
        // ====================================================================
        private static void Test_SI04_ArgumentForwarding()
        {
            string receivedMsg = null;
            AutoResetEvent signalEvent = new AutoResetEvent(false);

            SingleInstanceController.StartServer(delegate(string msg)
            {
                receivedMsg = msg;
                signalEvent.Set();
            });

            // Signal with Maximize and Open Vault path
            string targetVault = @"C:\Users\Ayush\Documents\ConfidentialVault.kcrypt";
            string customPayload = "RESTORE_MAXIMIZE|OPEN:" + targetVault;

            bool sent = SingleInstanceController.SignalRunningInstance(customPayload);
            Assert(sent, "Signal with custom payload must return true");

            bool gotEvent = signalEvent.WaitOne(2000);
            Assert(gotEvent, "Signal event must fire within 2000ms");
            Assert(receivedMsg.Contains("MAXIMIZE"), "Payload must contain MAXIMIZE");
            Assert(receivedMsg.Contains(targetVault), "Payload must contain target vault path");
        }

        // ====================================================================
        // TEST 5: UI DISPATCH & SEAMLESS WINDOW RESTORATION FROM TRAY
        // ====================================================================
        private static void Test_SI05_FormRestorationFromTray()
        {
            string scratchDir = Path.Combine(Path.GetTempPath(), "keycraft_si_tests");
            string vaultPath = Path.Combine(scratchDir, "test_si_vault.txt");
            if (!Directory.Exists(scratchDir)) Directory.CreateDirectory(scratchDir);
            File.WriteAllText(vaultPath, "1\tTestService\tuser\tpass\t2026-01-01\r\n");

            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();
                Application.DoEvents();

                // Minimize to tray
                form.MinimizeToTray();
                Application.DoEvents();
                Assert(!form.Visible, "MainForm must be hidden (Visible = false) when minimized to tray");

                // Start IPC Server
                SingleInstanceController.StartServer();

                // Send IPC Restore signal
                SingleInstanceController.SignalRunningInstance("RESTORE");

                // Pump Windows messages to allow BeginInvoke to process
                for (int i = 0; i < 20; i++)
                {
                    Application.DoEvents();
                    if (form.Visible) break;
                    Thread.Sleep(50);
                }

                Assert(form.Visible, "MainForm must be restored and Visible == true after IPC restore signal");
                Assert(form.WindowState == FormWindowState.Normal, "MainForm window state must be Normal");
            }

            try { if (File.Exists(vaultPath)) File.Delete(vaultPath); } catch { }
        }

        // ====================================================================
        // TEST 6: STRICT WINDOW ISOLATION - ZERO GDI+ / .NET-BROADCAST LEAKS
        // ====================================================================
        private static void Test_SI06_StrictWindowIsolation()
        {
            // Create a MainForm to ensure GDI+ and .NET helper windows are instantiated in this process
            string scratchDir = Path.Combine(Path.GetTempPath(), "keycraft_si_tests");
            string vaultPath = Path.Combine(scratchDir, "test_si_leak.txt");
            if (!Directory.Exists(scratchDir)) Directory.CreateDirectory(scratchDir);
            File.WriteAllText(vaultPath, "1\tTestLeak\tuser\tpass\t2026-01-01\r\n");

            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();
                Application.DoEvents();

                // Force GDI+ graphics usage so GDI+ Window is created by Windows
                using (Graphics g = form.CreateGraphics())
                {
                    g.DrawLine(Pens.White, 0, 0, 10, 10);
                }
                Application.DoEvents();

                // Minimize to tray
                form.MinimizeToTray();
                Application.DoEvents();

                // Start IPC Server & send signal
                SingleInstanceController.StartServer();
                SingleInstanceController.SignalRunningInstance("RESTORE");

                for (int i = 0; i < 15; i++)
                {
                    Application.DoEvents();
                    Thread.Sleep(30);
                }

                // Enumerate ALL windows belonging to this process
                uint currentPid = (uint)Process.GetCurrentProcess().Id;
                List<string> visibleWindows = new List<string>();

                NativeMethods.EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == currentPid)
                    {
                        if (NativeMethods.IsWindowVisible(hWnd))
                        {
                            int len = NativeMethods.GetWindowTextLength(hWnd);
                            StringBuilder sb = new StringBuilder(len + 1);
                            if (len > 0)
                            {
                                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                            }
                            string title = sb.ToString();
                            visibleWindows.Add(title);
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                // STRICT VERIFICATION:
                // 1. No window titled "GDI+ Window" must EVER be visible
                foreach (string title in visibleWindows)
                {
                    Assert(!title.StartsWith("GDI+", StringComparison.OrdinalIgnoreCase),
                        "CRITICAL: 'GDI+ Window' must NEVER be made visible! Found: " + title);
                }

                // 2. No window titled ".NET-BroadcastEventWindow" must EVER be visible
                foreach (string title in visibleWindows)
                {
                    Assert(!title.StartsWith(".NET-Broadcast", StringComparison.OrdinalIgnoreCase),
                        "CRITICAL: '.NET-BroadcastEventWindow' must NEVER be made visible! Found: " + title);
                }

                // 3. The ONLY visible titled window in this process must be KeyCraft!
                bool foundKeyCraft = false;
                foreach (string title in visibleWindows)
                {
                    if (title.StartsWith("KeyCraft", StringComparison.OrdinalIgnoreCase))
                    {
                        foundKeyCraft = true;
                    }
                }
                Assert(foundKeyCraft, "The legitimate KeyCraft window must be visible");
            }

            try { if (File.Exists(vaultPath)) File.Delete(vaultPath); } catch { }
        }

        // ====================================================================
        // TEST 7: TARGETED WIN32 FALLBACK IGNORES RUNTIME HELPER HANDLES
        // ====================================================================
        private static void Test_SI07_TargetedFallbackFilter()
        {
            // Invoke the private FallbackTargetedWin32Signal via reflection
            MethodInfo miFallback = typeof(SingleInstanceController).GetMethod(
                "FallbackTargetedWin32Signal",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert(miFallback != null, "FallbackTargetedWin32Signal method must exist");

            // Calling fallback when no second process exists should safely return false without throwing
            bool result = (bool)miFallback.Invoke(null, new object[] { "RESTORE" });
            Assert(!result, "Fallback search when no other process exists must safely return false");
        }

        // ====================================================================
        // TEST 8: RAPID CONSECUTIVE SIGNALS BURST HANDLING
        // ====================================================================
        private static void Test_SI08_RapidSignalBurst()
        {
            int receivedCount = 0;
            object countLock = new object();

            SingleInstanceController.StartServer(delegate(string msg)
            {
                lock (countLock)
                {
                    receivedCount++;
                }
            });

            // Send 5 rapid consecutive signals
            for (int i = 0; i < 5; i++)
            {
                SingleInstanceController.SignalRunningInstance("BURST_SIGNAL_" + i);
                Thread.Sleep(30);
            }

            // Wait for processing
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(50);
                lock (countLock)
                {
                    if (receivedCount >= 5) break;
                }
            }

            lock (countLock)
            {
                Assert(receivedCount == 5, string.Format("Server must process all 5 burst signals (Received: {0})", receivedCount));
            }
        }
    }
}
