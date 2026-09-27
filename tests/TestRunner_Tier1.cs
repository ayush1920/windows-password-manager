using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Tier 1: Automated Programmatic & Functional Engine Test Suite.
    /// Validates core algorithms, cryptography, models, services, debouncer, mutex, and file storage.
    /// </summary>
    class TestRunner_Tier1
    {
        private static int passedTests = 0;
        private static int failedTests = 0;

        [STAThread]
        static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("    KEYCRAFT TIER 1: PROGRAMMATIC & FUNCTIONAL ENGINE TEST SUITE  ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();

            RunTest("TC-01: SafeFileStorage Atomic Write & Backup", Test_SafeFileStorage);
            RunTest("TC-02: PBKDF2 Key Derivation (100,000 Iterations)", Test_PBKDF2_Derivation);
            RunTest("TC-03: AES-256-CBC Encryption & Decryption Roundtrip", Test_AES256_Roundtrip);
            RunTest("TC-04: HMAC-SHA256 Tamper Detection & Integrity", Test_HMAC_TamperDetection);
            RunTest("TC-05: Vault Zero-Knowledge In-Memory Key Zeroing", Test_SessionKey_Zeroing);
            RunTest("TC-06: Credential Model & Line Serialization", Test_CredentialSerialization);
            RunTest("TC-07: CredentialService Arbitrary Reordering & Sl No Remapping", Test_CredentialReordering);
            RunTest("TC-08: CredentialService Column-Specific Search Filtering", Test_SearchFiltering);
            RunTest("TC-09: DoublePressDebouncer High-Precision Timing & Jitter Filter", Test_DoublePressDebouncer);
            RunTest("TC-10: Single-Instance Mutex Enforcement", Test_SingleInstanceMutex);
            RunTest("TC-11: Global Hotkey Formatting & Win32 Availability Test", Test_GlobalHotkey);
            RunTest("TC-12: AppSettings Configuration Persistence", Test_AppSettings);

            Console.WriteLine();
            Console.WriteLine("------------------------------------------------------------------");
            if (failedTests == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ ALL TIER 1 TESTS PASSED! Total Passed: " + passedTests + ", Failed: 0");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✕ TIER 1 SUITE FAILED! Total Passed: " + passedTests + ", Failed: " + failedTests);
            }
            Console.ResetColor();
            Console.WriteLine("==================================================================");

            return failedTests == 0 ? 0 : 1;
        }

        private static void RunTest(string testName, Action testAction)
        {
            Console.Write(testName.PadRight(60) + " ... ");
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS]");
                Console.ResetColor();
                passedTests++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL]");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("   Error: " + ex.Message);
                Console.ResetColor();
                failedTests++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        // ==========================================
        // TEST IMPLEMENTATIONS
        // ==========================================

        private static void Test_SafeFileStorage()
        {
            string testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "test_atomic.txt");
            string backupFile = testFile + ".bak";

            try
            {
                if (File.Exists(testFile)) File.Delete(testFile);
                if (File.Exists(backupFile)) File.Delete(backupFile);

                // 1. Initial atomic write
                string initial = "Initial atomic content 12345";
                SafeFileStorage.WriteAllTextAtomic(testFile, initial, true);
                Assert(File.Exists(testFile), "File must exist after write");
                Assert(SafeFileStorage.ReadAllTextSafe(testFile) == initial, "Content must match");

                // 2. Second atomic write (creates backup)
                string updated = "Updated atomic content 67890";
                SafeFileStorage.WriteAllTextAtomic(testFile, updated, true);
                Assert(SafeFileStorage.ReadAllTextSafe(testFile) == updated, "Updated content must match");
                Assert(File.Exists(backupFile), "Backup file must be created on update");
                Assert(File.ReadAllText(backupFile) == initial, "Backup file must contain previous content");

                // 3. Fallback restore from backup
                File.Delete(testFile);
                Assert(SafeFileStorage.ReadAllTextSafe(testFile) == initial, "Safe read must fallback to backup");
            }
            finally
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                try { if (File.Exists(backupFile)) File.Delete(backupFile); } catch { }
            }
        }

        private static void Test_PBKDF2_Derivation()
        {
            byte[] salt = new byte[16];
            for (int i = 0; i < 16; i++) salt[i] = (byte)(i + 1);

            byte[] encKey1, authKey1;
            VaultSecurity.DeriveKeys("MasterPassword123!", salt, out encKey1, out authKey1);

            Assert(encKey1 != null && encKey1.Length == 32, "AES encryption key must be 32 bytes (256 bits)");
            Assert(authKey1 != null && authKey1.Length == 32, "HMAC auth key must be 32 bytes (256 bits)");

            // Derive again with same password and salt -> must yield identical keys
            byte[] encKey2, authKey2;
            VaultSecurity.DeriveKeys("MasterPassword123!", salt, out encKey2, out authKey2);

            for (int i = 0; i < 32; i++)
            {
                Assert(encKey1[i] == encKey2[i], "PBKDF2 derivation must be deterministic");
                Assert(authKey1[i] == authKey2[i], "Auth key derivation must be deterministic");
            }

            // Derive with different password -> must yield completely different keys
            byte[] encKey3, authKey3;
            VaultSecurity.DeriveKeys("DifferentPassword456!", salt, out encKey3, out authKey3);
            bool isDifferent = false;
            for (int i = 0; i < 32; i++)
            {
                if (encKey1[i] != encKey3[i]) { isDifferent = true; break; }
            }
            Assert(isDifferent, "Different password must yield different keys");
        }

        private static void Test_AES256_Roundtrip()
        {
            byte[] key = new byte[32];
            new RNGCryptoServiceProvider().GetBytes(key);

            string originalText = "TopSecretCredentials\tAdmin\tSuperPassword!@#123\nSecondLine\tUser2\tPass2";

            byte[] iv;
            string ciphertext = VaultSecurity.EncryptString(originalText, key, out iv);

            Assert(!string.IsNullOrEmpty(ciphertext), "Ciphertext must not be empty");
            Assert(iv != null && iv.Length == 16, "IV must be 16 bytes");
            Assert(ciphertext != originalText, "Ciphertext must be scrambled");

            string decrypted = VaultSecurity.DecryptString(ciphertext, key, iv);
            Assert(decrypted == originalText, "Decrypted plaintext must match original plaintext exactly");
        }

        private static void Test_HMAC_TamperDetection()
        {
            string testVault = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "test_tamper.txt");
            try
            {
                if (File.Exists(testVault)) File.Delete(testVault);

                string masterPass = "SecureVault2026!";
                string plainData = "1\tServiceA\tUserA\tPassA\tNotesA";

                VaultSecurity.InitializeAndEncryptVault(testVault, masterPass, plainData);

                // Verify valid unlock
                string unlockedText;
                bool ok = VaultSecurity.UnlockVault(testVault, masterPass, out unlockedText);
                Assert(ok, "Original vault must unlock successfully");

                // Tamper 1: Corrupt the HMAC Verify token
                string fileContent = File.ReadAllText(testVault, Encoding.UTF8);
                int verifyIdx = fileContent.IndexOf("Verify:");
                Assert(verifyIdx > 0, "Verify tag must exist");

                char corruptedChar = fileContent[verifyIdx + 10] == 'A' ? 'B' : 'A';
                string tamperedContent = fileContent.Substring(0, verifyIdx + 10) + corruptedChar + fileContent.Substring(verifyIdx + 11);
                File.WriteAllText(testVault, tamperedContent, Encoding.UTF8);

                // Attempt unlock of tampered verify token
                string tamperedUnlocked;
                bool tamperedOk = VaultSecurity.UnlockVault(testVault, masterPass, out tamperedUnlocked);
                Assert(!tamperedOk, "Vault with tampered HMAC verify token MUST fail unlock verification");
            }
            finally
            {
                try { if (File.Exists(testVault)) File.Delete(testVault); } catch { }
                try { if (File.Exists(testVault + ".bak")) File.Delete(testVault + ".bak"); } catch { }
            }
        }

        private static void Test_SessionKey_Zeroing()
        {
            byte[] encKey = new byte[32];
            byte[] authKey = new byte[32];
            byte[] salt = new byte[16];
            for (int i = 0; i < 32; i++) { encKey[i] = 0xAA; authKey[i] = 0xBB; }
            for (int i = 0; i < 16; i++) { salt[i] = 0xCC; }

            VaultSecurity.SetSession(encKey, authKey, salt);
            Assert(VaultSecurity.IsUnlocked, "Vault must be unlocked after SetSession");

            VaultSecurity.LockSession();
            Assert(!VaultSecurity.IsUnlocked, "Vault must be locked after LockSession");

            // Verify session arrays were wiped
            for (int i = 0; i < 32; i++)
            {
                Assert(encKey[i] == 0, "Encryption key in RAM must be zeroed upon lock");
                Assert(authKey[i] == 0, "Auth key in RAM must be zeroed upon lock");
            }
            for (int i = 0; i < 16; i++)
            {
                Assert(salt[i] == 0, "Salt in RAM must be zeroed upon lock");
            }
        }

        private static void Test_CredentialSerialization()
        {
            Credential c = new Credential("GitHub", "john_doe", "P@ssw0rd!#$", 1);
            c.Id = "id-123";
            string line = CredentialRepository.SerializeLine(c);

            Credential deserialized = CredentialRepository.DeserializeLine(line, 1);
            Assert(deserialized != null, "Deserialized credential must not be null");
            Assert(deserialized.Id == "id-123", "Id must match");
            Assert(deserialized.SerialNo == 1, "SerialNo must match");
            Assert(deserialized.Service == "GitHub", "Service must match");
            Assert(deserialized.Username == "john_doe", "Username must match");
            Assert(deserialized.Password == "P@ssw0rd!#$", "Password must match");
        }

        private static void Test_CredentialReordering()
        {
            string testRepoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "test_reorder.txt");
            try
            {
                if (File.Exists(testRepoPath)) File.Delete(testRepoPath);

                CredentialRepository repo = new CredentialRepository(testRepoPath);
                Credential c1 = new Credential("FirstService", "user1", "pass1", 1); c1.Id = "1"; repo.Add(c1);
                Credential c2 = new Credential("SecondService", "user2", "pass2", 2); c2.Id = "2"; repo.Add(c2);
                Credential c3 = new Credential("ThirdService", "user3", "pass3", 3); c3.Id = "3"; repo.Add(c3);
                Credential c4 = new Credential("FourthService", "user4", "pass4", 4); c4.Id = "4"; repo.Add(c4);

                CredentialService svc = new CredentialService(repo);

                // Move FourthService (item with Id "4") to Serial No 1 (top of list)
                bool moved = svc.ReorderCredential("4", 1);
                Assert(moved, "ReorderCredential must succeed");

                List<Credential> updated = svc.LoadAll();
                Assert(updated.Count == 4, "Must have 4 credentials");
                Assert(updated[0].Service == "FourthService" && updated[0].SerialNo == 1, "FourthService must now be SerialNo 1");
                Assert(updated[1].Service == "FirstService" && updated[1].SerialNo == 2, "FirstService must now be SerialNo 2");
                Assert(updated[2].Service == "SecondService" && updated[2].SerialNo == 3, "SecondService must now be SerialNo 3");
                Assert(updated[3].Service == "ThirdService" && updated[3].SerialNo == 4, "ThirdService must now be SerialNo 4");

                // Move FourthService down to position 2
                svc.ReorderCredential("4", 2);
                updated = svc.LoadAll();
                Assert(updated[1].Service == "FourthService" && updated[1].SerialNo == 2, "FourthService must move to position 2");
            }
            finally
            {
                try { if (File.Exists(testRepoPath)) File.Delete(testRepoPath); } catch { }
                try { if (File.Exists(testRepoPath + ".bak")) File.Delete(testRepoPath + ".bak"); } catch { }
            }
        }

        private static void Test_SearchFiltering()
        {
            string testSearchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "test_search.txt");
            try
            {
                if (File.Exists(testSearchPath)) File.Delete(testSearchPath);

                CredentialRepository repo = new CredentialRepository(testSearchPath);
                repo.Add(new Credential("AWS Cloud", "admin", "p1", 1));
                repo.Add(new Credential("GitHub Repo", "octocat", "p2", 2));
                repo.Add(new Credential("Google Work", "work@company.com", "p3", 3));
                repo.Add(new Credential("ProtonMail", "secure@pm.me", "p4", 4));

                CredentialService svc = new CredentialService(repo);
                List<Credential> all = svc.LoadAll();

                // 1. Search by Sl No
                List<Credential> r1 = svc.Filter(all, "2", "Sl No");
                Assert(r1.Count == 1 && r1[0].Service == "GitHub Repo", "Search by Sl No 2 must return GitHub Repo");

                // 2. Search by Service
                List<Credential> r2 = svc.Filter(all, "git", "Service");
                Assert(r2.Count == 1 && r2[0].Service == "GitHub Repo", "Search by Service 'git' must return GitHub Repo");

                // 3. Search by Username
                List<Credential> r3 = svc.Filter(all, "company", "Username");
                Assert(r3.Count == 1 && r3[0].Service == "Google Work", "Search by Username 'company' must return Google Work");

                // 4. Empty search query returns all
                List<Credential> r4 = svc.Filter(all, "", "Service");
                Assert(r4.Count == 4, "Empty search must return all credentials");
            }
            finally
            {
                try { if (File.Exists(testSearchPath)) File.Delete(testSearchPath); } catch { }
                try { if (File.Exists(testSearchPath + ".bak")) File.Delete(testSearchPath + ".bak"); } catch { }
            }
        }

        private static void Test_DoublePressDebouncer()
        {
            DoublePressDebouncer debouncer = new DoublePressDebouncer(500, 60);

            // Press 1
            EscapePressResult r1 = debouncer.RegisterPress();
            Assert(r1 == EscapePressResult.SinglePress, "First press must be SinglePress");

            // Rapid bounce within 20ms (<60ms) -> must be IgnoredBounce
            Thread.Sleep(20);
            EscapePressResult r2 = debouncer.RegisterPress();
            Assert(r2 == EscapePressResult.IgnoredBounce, "Press at 20ms must be IgnoredBounce");

            // Consecutive second tap at 180ms (<500ms) -> must be DoublePress
            Thread.Sleep(160);
            EscapePressResult r3 = debouncer.RegisterPress();
            Assert(r3 == EscapePressResult.DoublePress, "Press at 180ms must be DoublePress");

            // Test timeout: Press 1, wait 600ms (>500ms), Press 2
            debouncer.Reset();
            EscapePressResult r4 = debouncer.RegisterPress();
            Assert(r4 == EscapePressResult.SinglePress, "Initial press after reset must be SinglePress");

            Thread.Sleep(600);
            EscapePressResult r5 = debouncer.RegisterPress();
            Assert(r5 == EscapePressResult.SinglePress, "Press after 600ms must be treated as independent SinglePress");
        }

        private static void Test_SingleInstanceMutex()
        {
            System.Threading.Mutex m1;
            bool ok1 = SingleInstanceController.TryAcquireMutex(out m1);
            Assert(ok1, "First mutex acquisition must succeed");

            System.Threading.Mutex m2;
            bool ok2 = SingleInstanceController.TryAcquireMutex(out m2);
            Assert(!ok2, "Second mutex acquisition while first is held MUST fail");

            if (m1 != null)
            {
                m1.ReleaseMutex();
                m1.Close();
            }
            if (m2 != null)
            {
                m2.Close();
            }
        }

        private static void Test_GlobalHotkey()
        {
            string fmt1 = GlobalHotkeyManager.FormatHotkey(GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, Keys.K);
            Assert(fmt1 == "Ctrl + Alt + K", "FormatHotkey for Ctrl+Alt+K must match: " + fmt1);

            string fmt2 = GlobalHotkeyManager.FormatHotkey(GlobalHotkeyManager.MOD_WIN | GlobalHotkeyManager.MOD_SHIFT, Keys.P);
            Assert(fmt2 == "Win + Shift + P", "FormatHotkey for Win+Shift+P must match: " + fmt2);

            // Win32 Hotkey Test
            using (Form testForm = new Form())
            {
                testForm.CreateControl();
                string warning;
                bool available = GlobalHotkeyManager.TestHotkeyAvailability(testForm.Handle, GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, Keys.K, out warning);
                // Should either be available or give a clean warning if system already uses it
                if (!available)
                {
                    Assert(!string.IsNullOrEmpty(warning), "If unavailable, warning message must be populated");
                }
            }
        }

        private static void Test_AppSettings()
        {
            AppSettings settings = new AppSettings();
            settings.RunOnStartup = false;
            settings.CloseToTray = true;
            settings.HotkeyEnabled = true;
            settings.HotkeyModifiers = GlobalHotkeyManager.MOD_WIN | GlobalHotkeyManager.MOD_ALT;
            settings.HotkeyKey = Keys.P;

            settings.Save();

            AppSettings reloaded = AppSettings.Load();
            Assert(reloaded.RunOnStartup == false, "RunOnStartup must match");
            Assert(reloaded.CloseToTray == true, "CloseToTray must match");
            Assert(reloaded.HotkeyEnabled == true, "HotkeyEnabled must match");
            Assert(reloaded.HotkeyModifiers == (GlobalHotkeyManager.MOD_WIN | GlobalHotkeyManager.MOD_ALT), "HotkeyModifiers must match");
            Assert(reloaded.HotkeyKey == Keys.P, "HotkeyKey must match");

            // Reset back to defaults
            settings.CloseToTray = false;
            settings.HotkeyModifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT;
            settings.HotkeyKey = Keys.K;
            settings.Save();
        }
    }
}
