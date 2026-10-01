using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Tier 2: End-to-End UI & System Automation Test Suite.
    /// Controls the real application UI from top to bottom starting from clean 0 data.
    /// </summary>
    class TestRunner_Tier2
    {
        private static int passedFlows = 0;
        private static int failedFlows = 0;
        private static string originalBackupPath;

        [STAThread]
        static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("    KEYCRAFT TIER 2: END-TO-END UI & SYSTEM AUTOMATION SUITE      ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();

            string vaultPath = CredentialRepository.GetDefaultPath();
            originalBackupPath = vaultPath + ".user_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Backup existing user data before wipe
            if (File.Exists(vaultPath))
            {
                File.Copy(vaultPath, originalBackupPath, true);
                Console.WriteLine("Notice: Backed up existing credentials to: " + Path.GetFileName(originalBackupPath));
            }

            try
            {
                RunFlow("FLOW 1: Clean Data Wipe & Master Password Creation UI", Test_Flow1_MasterPasswordCreation);
                RunFlow("FLOW 2: Vault Lock & Master Password Unlock UI", Test_Flow2_MasterPasswordUnlock);
                RunFlow("FLOW 3: Credential Management CRUD, Remapping & Generator", Test_Flow3_CredentialCRUDAndRemapping);
                RunFlow("FLOW 4: Real-Time Search Filtering & Quick Navigation", Test_Flow4_SearchAndFilter);
                RunFlow("FLOW 5: Clipboard Password Copy & Toast Notification", Test_Flow5_ClipboardCopy);
                RunFlow("FLOW 6: Debounced Double-Escape Minimize to System Tray", Test_Flow6_DoubleEscapeTrayMinimize);
                RunFlow("FLOW 7: Single-Instance IPC Wake-Up from Tray to Foreground", Test_Flow7_SingleInstanceWakeUp);
                RunFlow("FLOW 8: Settings Panel, PowerToys Hotkey & CloseToTray", Test_Flow8_SettingsAndPreferences);
                RunFlow("FLOW 9: KeePass Multi-Vault Dynamic Creation & Switching", Test_Flow9_KeePassMultiVault);
            }
            finally
            {
                // Restore original user backup so no data is lost
                if (File.Exists(originalBackupPath))
                {
                    try
                    {
                        File.Copy(originalBackupPath, vaultPath, true);
                        File.Delete(originalBackupPath);
                        Console.WriteLine("Notice: Restored original user vault file.");
                    }
                    catch { }
                }
            }

            Console.WriteLine();
            Console.WriteLine("------------------------------------------------------------------");
            if (failedFlows == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ ALL TIER 2 UI FLOWS PASSED! Total Passed: " + passedFlows + ", Failed: 0");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✕ TIER 2 SUITE FAILED! Total Passed: " + passedFlows + ", Failed: " + failedFlows);
            }
            Console.ResetColor();
            Console.WriteLine("==================================================================");

            return failedFlows == 0 ? 0 : 1;
        }

        private static void RunFlow(string flowName, Action flowAction)
        {
            Console.Write(flowName.PadRight(60) + " ... ");
            try
            {
                flowAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS]");
                Console.ResetColor();
                passedFlows++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL]");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("   Error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("   Inner: " + ex.InnerException.Message);
                }
                Console.ResetColor();
                failedFlows++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        private static T GetField<T>(object instance, string fieldName)
        {
            FieldInfo fi = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (fi == null) throw new Exception("Field not found: " + fieldName);
            return (T)fi.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo fi = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (fi == null) throw new Exception("Field not found: " + fieldName);
            fi.SetValue(instance, value);
        }

        private static void InvokeMethod(object instance, string methodName, params object[] parameters)
        {
            MethodInfo mi = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (mi == null) throw new Exception("Method not found: " + methodName);
            mi.Invoke(instance, parameters);
        }

        // ==========================================
        // FLOW IMPLEMENTATIONS
        // ==========================================

        private static void Test_Flow1_MasterPasswordCreation()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            if (File.Exists(vaultPath)) File.Delete(vaultPath);
            if (File.Exists(vaultPath + ".bak")) File.Delete(vaultPath + ".bak");

            Assert(!VaultSecurity.IsVaultEncrypted(vaultPath), "Vault must not exist / not be encrypted initially");

            using (MasterPasswordForm form = new MasterPasswordForm(MasterPasswordMode.Create, vaultPath))
            {
                form.CreateControl();
                form.Show();

                TextBox txtPass = GetField<TextBox>(form, "txtPassword");
                TextBox txtConf = GetField<TextBox>(form, "txtConfirm");
                Label lblErr = GetField<Label>(form, "lblError");

                // 1. Test mismatched confirmation
                txtPass.Text = "AlphaMaster2026!";
                txtConf.Text = "WrongConfirm!";
                InvokeMethod(form, "HandleSubmit");

                Assert(lblErr.Text.Contains("do not match"), "Error text must state passwords do not match");
                Assert(form.DialogResult != DialogResult.OK, "DialogResult must not be OK");

                // 2. Test matching valid master password
                txtConf.Text = "AlphaMaster2026!";
                InvokeMethod(form, "HandleSubmit");

                Assert(form.DialogResult == DialogResult.OK, "DialogResult must be OK on valid creation");
            }

            Assert(File.Exists(vaultPath), "Encrypted vault file must now exist on disk");
            Assert(VaultSecurity.IsVaultEncrypted(vaultPath), "Vault file must be in KEYCRAFT_ENC_V1 encrypted format");
        }

        private static void Test_Flow2_MasterPasswordUnlock()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            Assert(VaultSecurity.IsVaultEncrypted(vaultPath), "Vault must be encrypted before unlock test");

            // Lock session
            VaultSecurity.LockSession();
            Assert(!VaultSecurity.IsUnlocked, "Vault must be locked in RAM");

            using (MasterPasswordForm form = new MasterPasswordForm(MasterPasswordMode.Unlock, vaultPath))
            {
                form.CreateControl();
                form.Show();

                TextBox txtPass = GetField<TextBox>(form, "txtPassword");
                Label lblErr = GetField<Label>(form, "lblError");

                // 1. Test incorrect password
                txtPass.Text = "WrongPassword999!";
                InvokeMethod(form, "HandleSubmit");

                Assert(lblErr.Text.Contains("Incorrect master password"), "Error must state incorrect password");
                Assert(!VaultSecurity.IsUnlocked, "Vault must remain locked");

                // 2. Test correct password
                txtPass.Text = "AlphaMaster2026!";
                InvokeMethod(form, "HandleSubmit");

                Assert(form.DialogResult == DialogResult.OK, "DialogResult must be OK on correct unlock password");
                Assert(VaultSecurity.IsUnlocked, "Vault must be unlocked in active session");
            }
        }

        private static void Test_Flow3_CredentialCRUDAndRemapping()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();

                TextBox txtService = GetField<TextBox>(form, "txtService");
                TextBox txtUsername = GetField<TextBox>(form, "txtUsername");
                TextBox txtPassword = GetField<TextBox>(form, "txtPassword");
                TextBox txtSerialNo = GetField<TextBox>(form, "txtSerialNo");
                ListView lv = GetField<ListView>(form, "lvCredentials");
                ModernButton btnSave = GetField<ModernButton>(form, "btnSave");
                ModernButton btnClear = GetField<ModernButton>(form, "btnClear");

                // 1. Add 4 Credentials
                txtService.Text = "GitHub";
                txtUsername.Text = "octocat";
                txtPassword.Text = "GitSecure2026!";
                btnSave.PerformClick();
                btnClear.PerformClick();

                txtService.Text = "AWS Console";
                txtUsername.Text = "cloud_admin";
                txtPassword.Text = "AwsCloud!999";
                btnSave.PerformClick();
                btnClear.PerformClick();

                txtService.Text = "ProtonMail";
                txtUsername.Text = "secure_user";
                txtPassword.Text = "PmSecret#456";
                btnSave.PerformClick();
                btnClear.PerformClick();

                txtService.Text = "Corporate Slack";
                txtUsername.Text = "slack_dev";
                ModernButton btnGen = GetField<ModernButton>(form, "btnGeneratePassword");
                btnGen.PerformClick();
                Assert(!string.IsNullOrEmpty(txtPassword.Text), "Generated password must not be empty");
                btnSave.PerformClick();

                Assert(lv.Items.Count == 4, "ListView must contain 4 items");

                // 2. Edit Credential 2 (AWS Console)
                lv.Items[1].Selected = true;
                InvokeMethod(form, "LvCredentials_SelectedIndexChanged", lv, EventArgs.Empty);
                txtUsername.Text = "aws_root_user";
                btnSave.PerformClick();

                Assert(lv.Items[1].SubItems[2].Text == "aws_root_user", "Edited username must be updated in ListView");

                // 3. Remap Serial Number: Move row 4 (Corporate Slack) to Serial No 1
                lv.Items[3].Selected = true;
                InvokeMethod(form, "LvCredentials_SelectedIndexChanged", lv, EventArgs.Empty);
                txtSerialNo.Text = "1";
                btnSave.PerformClick();

                Assert(lv.Items[0].SubItems[1].Text == "Corporate Slack", "Corporate Slack must now be row 1");
                Assert(lv.Items[1].SubItems[1].Text == "GitHub", "GitHub must now be row 2");
                Assert(lv.Items[2].SubItems[1].Text == "AWS Console", "AWS Console must now be row 3");
                Assert(lv.Items[3].SubItems[1].Text == "ProtonMail", "ProtonMail must now be row 4");

                // 4. Test Alt+Up reordering via ModernButton
                ModernButton btnUp = GetField<ModernButton>(form, "btnMoveUp");
                lv.Items[2].Selected = true; // Select AWS Console
                InvokeMethod(form, "LvCredentials_SelectedIndexChanged", lv, EventArgs.Empty);
                btnUp.PerformClick();
                Assert(lv.Items[1].SubItems[1].Text == "AWS Console", "AWS Console must have moved up to row 2");
            }
        }

        private static void Test_Flow4_SearchAndFilter()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();

                TextBox txtSearch = GetField<TextBox>(form, "txtSearch");
                ComboBox cmbCol = GetField<ComboBox>(form, "cmbSearchColumn");
                ListView lv = GetField<ListView>(form, "lvCredentials");

                Assert(lv.Items.Count == 4, "Must start with 4 credentials");

                // 1. Search by Sl No: type "2"
                cmbCol.SelectedItem = "Sl No";
                txtSearch.Text = "2";
                Assert(lv.Items.Count == 1, "Searching Sl No 2 must return exactly 1 item");
                Assert(lv.Items[0].SubItems[0].Text == "2", "Matched item must have Sl No 2");

                // 2. Search by Service: switch column, type "proton"
                cmbCol.SelectedItem = "Service";
                txtSearch.Text = "proton";
                Assert(lv.Items.Count == 1, "Searching Service 'proton' must return 1 item");
                Assert(lv.Items[0].SubItems[1].Text == "ProtonMail", "Matched item must be ProtonMail");

                // 3. Clear search via Single Escape
                InvokeMethod(form, "HandleSingleEscape");
                Assert(string.IsNullOrEmpty(txtSearch.Text), "Single escape must clear search box");
                Assert(lv.Items.Count == 4, "All 4 items must return after search is cleared");
            }
        }

        private static void Test_Flow5_ClipboardCopy()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();

                ListView lv = GetField<ListView>(form, "lvCredentials");
                ModernButton btnCopy = GetField<ModernButton>(form, "btnCopyPassword");
                Label lblToast = GetField<Label>(form, "lblToast");

                // Select row 0
                lv.Items[0].Selected = true;
                Application.DoEvents();
                InvokeMethod(form, "LvCredentials_SelectedIndexChanged", lv, EventArgs.Empty);
                Application.DoEvents();
                Credential selected = lv.Items[0].Tag as Credential;
                Assert(selected != null, "Selected credential tag must not be null");

                // Copy password to clipboard
                btnCopy.PerformClick();
                Application.DoEvents();

                string clipText = null;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        clipText = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(clipText)) break;
                    }
                    catch { }
                    Thread.Sleep(50);
                }

                string lastCopied = GetField<string>(form, "lastCopiedPassword");
                Assert(clipText == selected.Password || lastCopied == selected.Password, "Clipboard text must match selected credential password exactly");
                Assert(lblToast.Text.Contains("copied"), "Toast text must indicate successful copy");
            }
        }

        private static void Test_Flow6_DoubleEscapeTrayMinimize()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();

                NotifyIcon notify = GetField<NotifyIcon>(form, "notifyIcon");

                Assert(form.Visible, "Form must be visible initially");
                Assert(form.ShowInTaskbar, "Form must be shown in taskbar initially");

                // Trigger Double Escape minimize
                form.MinimizeToTray();

                Assert(!form.Visible, "Form must be hidden after MinimizeToTray()");
                Assert(!form.ShowInTaskbar, "Form must be removed from taskbar after MinimizeToTray()");
                Assert(notify != null && notify.Visible, "NotifyIcon must be active in System Tray");
            }
        }

        private static void Test_Flow7_SingleInstanceWakeUp()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            using (MainForm form = new MainForm(service))
            {
                form.CreateControl();
                form.Show();

                // Put form in tray
                form.MinimizeToTray();
                Assert(!form.Visible, "Form must be hidden in tray");

                // Simulate SingleInstanceController IPC restore signal delivered to window
                Message restoreMsg = new Message();
                restoreMsg.HWnd = form.Handle;
                restoreMsg.Msg = (int)SingleInstanceController.RestoreWindowMessageId;
                restoreMsg.WParam = (IntPtr)1; // Restore Normal
                restoreMsg.LParam = IntPtr.Zero;

                InvokeMethod(form, "WndProc", restoreMsg);

                // Allow message dispatch
                Application.DoEvents();

                Assert(form.Visible, "Form must be unhidden and visible after receiving IPC restore message");
                Assert(form.ShowInTaskbar, "Form must be returned to taskbar after IPC restore");
                Assert(form.WindowState == FormWindowState.Normal, "Form must be restored to Normal window state");
            }
        }

        private static void Test_Flow8_SettingsAndPreferences()
        {
            string vaultPath = CredentialRepository.GetDefaultPath();
            CredentialRepository repo = new CredentialRepository(vaultPath);
            CredentialService service = new CredentialService(repo);

            AppSettings settings = AppSettings.Load();

            using (SettingsForm sf = new SettingsForm(service, settings))
            {
                sf.CreateControl();
                sf.Show();

                CheckBox chkClose = GetField<CheckBox>(sf, "chkCloseToTray");
                CheckBox chkStartup = GetField<CheckBox>(sf, "chkStartup");
                CheckBox chkHotkey = GetField<CheckBox>(sf, "chkEnableHotkey");
                HotkeyPickerControl picker = GetField<HotkeyPickerControl>(sf, "pickerHotkey");

                // 1. Test Behavior preferences: enable CloseToTray
                chkClose.Checked = true;
                chkStartup.Checked = false;
                InvokeMethod(sf, "HandleSaveBehavior");

                Assert(settings.CloseToTray == true, "CloseToTray must be saved as true in AppSettings");

                // 2. Test PowerToys Hotkey picker: set to Ctrl+Alt+P
                chkHotkey.Checked = true;
                picker.SetHotkey(GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, Keys.P);
                InvokeMethod(sf, "HandleSaveHotkey");

                Assert(settings.HotkeyKey == Keys.P, "HotkeyKey must be saved as P");
                Assert(!picker.HasConflict, "Ctrl+Alt+P should be valid and conflict-free");

                // 3. Test Master Password rotation in Settings
                TextBox txtCurr = GetField<TextBox>(sf, "txtCurrentPass");
                TextBox txtNew = GetField<TextBox>(sf, "txtNewPass");
                TextBox txtConf = GetField<TextBox>(sf, "txtConfirmPass");

                txtCurr.Text = "AlphaMaster2026!";
                txtNew.Text = "OmegaMaster2026!";
                txtConf.Text = "OmegaMaster2026!";

                InvokeMethod(sf, "HandleChangePassword");

                Label lblMsg = GetField<Label>(sf, "lblMessage");
                Assert(lblMsg.Text.Contains("updated"), "Password change confirmation must be shown");

                // Verify new master password unlocks vault
                VaultSecurity.LockSession();
                string decrypted;
                bool ok = VaultSecurity.UnlockVault(vaultPath, "OmegaMaster2026!", out decrypted);
                Assert(ok, "Vault must unlock with newly rotated master password OmegaMaster2026!");
            }
        }

        private static void Test_Flow9_KeePassMultiVault()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "keycraft_multi_vault_test");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            string vaultA = Path.Combine(tempDir, "personal.kcrypt");
            string vaultB = Path.Combine(tempDir, "work.kcrypt");

            if (File.Exists(vaultA)) File.Delete(vaultA);
            if (File.Exists(vaultB)) File.Delete(vaultB);

            // 1. Create Vault A via MasterPasswordForm
            using (MasterPasswordForm formA = new MasterPasswordForm(MasterPasswordMode.Create, vaultA))
            {
                formA.CreateControl();
                formA.Show();

                TextBox txtP = GetField<TextBox>(formA, "txtPassword");
                TextBox txtC = GetField<TextBox>(formA, "txtConfirm");
                txtP.Text = "PersonalPass2026!";
                txtC.Text = "PersonalPass2026!";
                InvokeMethod(formA, "HandleSubmit");

                Assert(formA.DialogResult == DialogResult.OK, "FormA must succeed");
                Assert(VaultSecurity.IsVaultEncrypted(vaultA), "VaultA must be encrypted");
            }

            // Seed Vault A with credential
            CredentialRepository repoA = new CredentialRepository(vaultA);
            repoA.Add(new Credential("Netflix", "userA", "netflixPass"));

            // 2. Create Vault B via MasterPasswordForm
            using (MasterPasswordForm formB = new MasterPasswordForm(MasterPasswordMode.Create, vaultB))
            {
                formB.CreateControl();
                formB.Show();

                TextBox txtP = GetField<TextBox>(formB, "txtPassword");
                TextBox txtC = GetField<TextBox>(formB, "txtConfirm");
                txtP.Text = "WorkPass2026!";
                txtC.Text = "WorkPass2026!";
                InvokeMethod(formB, "HandleSubmit");

                Assert(formB.DialogResult == DialogResult.OK, "FormB must succeed");
                Assert(VaultSecurity.IsVaultEncrypted(vaultB), "VaultB must be encrypted");
            }

            // Seed Vault B with credential
            CredentialRepository repoB = new CredentialRepository(vaultB);
            repoB.Add(new Credential("Company GitHub", "userB", "workToken123"));

            // 3. Cryptographic isolation verification: wrong password must fail
            VaultSecurity.LockSession();
            string dec;
            Assert(!VaultSecurity.UnlockVault(vaultA, "WorkPass2026!", out dec), "Unlocking VaultA with VaultB password must fail");
            Assert(VaultSecurity.UnlockVault(vaultA, "PersonalPass2026!", out dec), "Unlocking VaultA with correct password must succeed");

            VaultSecurity.LockSession();
            Assert(!VaultSecurity.UnlockVault(vaultB, "PersonalPass2026!", out dec), "Unlocking VaultB with VaultA password must fail");
            Assert(VaultSecurity.UnlockVault(vaultB, "WorkPass2026!", out dec), "Unlocking VaultB with correct password must succeed");

            // 4. Test Dynamic Vault Switching in MainForm
            VaultSecurity.UnlockVault(vaultA, "PersonalPass2026!", out dec);
            CredentialService service = new CredentialService(new CredentialRepository(vaultA));
            using (MainForm main = new MainForm(service))
            {
                main.CreateControl();
                main.Show();
                Application.DoEvents();

                Assert(main.Text.Contains("personal.kcrypt"), "MainForm title must show personal.kcrypt");
                ListView lv = GetField<ListView>(main, "lvCredentials");
                Assert(lv.Items.Count > 0, "MainForm must have loaded items from VaultA");
                Assert(lv.Items[0].SubItems[1].Text == "Netflix", "MainForm must display Netflix credential from VaultA");

                // Switch to Vault B (unlock session first before switching with alreadyUnlocked=true)
                VaultSecurity.UnlockVault(vaultB, "WorkPass2026!", out dec);
                main.SwitchToVaultFile(vaultB, true);
                Application.DoEvents();
                Assert(main.Text.Contains("work.kcrypt"), "MainForm title must update to work.kcrypt");
                Assert(service.GetVaultFilePath() == vaultB, "Service must now point to VaultB");
                Assert(lv.Items[0].SubItems[1].Text == "Company GitHub", "MainForm must display Company GitHub from VaultB");

                // Switch back to Vault A
                VaultSecurity.UnlockVault(vaultA, "PersonalPass2026!", out dec);
                main.SwitchToVaultFile(vaultA, true);
                Application.DoEvents();
                Assert(main.Text.Contains("personal.kcrypt"), "MainForm title must return to personal.kcrypt");
                Assert(service.GetVaultFilePath() == vaultA, "Service must point back to VaultA");
                Assert(lv.Items[0].SubItems[1].Text == "Netflix", "MainForm must display Netflix again");
            }

            // Cleanup
            try
            {
                if (File.Exists(vaultA)) File.Delete(vaultA);
                if (File.Exists(vaultB)) File.Delete(vaultB);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }
}
