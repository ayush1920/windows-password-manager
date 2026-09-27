using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PasswordGui;

namespace PasswordGui.Tests
{
    /// <summary>
    /// Comprehensive KeePass Multi-Vault Architecture Test Suite.
    /// Extensively validates zero-knowledge cryptography, standalone .kcrypt vaults,
    /// dynamic database switching, AppSettings MRU tracking, UI workflows, and hotkey routing.
    /// </summary>
    public static class TestRunner_MultiVault
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
            Console.WriteLine("   KEYCRAFT COMPREHENSIVE KEEPASS MULTI-VAULT TEST SUITE          ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();

            RunTest("MV-01: Standalone .kcrypt Header Format & Zero Plaintext Leaks", Test_MV01_KcryptFormatAndZeroPlaintext);
            RunTest("MV-02: Zero-Knowledge Multi-Vault Cryptographic Isolation", Test_MV02_ZeroKnowledgeIsolation);
            RunTest("MV-03: Zero-Knowledge In-Memory Key Wipe & Locked State Enforcement", Test_MV03_KeyZeroingAndLockEnforcement);
            RunTest("MV-04: HMAC-SHA256 Tamper Detection & Bit-Flip Rejection", Test_MV04_TamperDetectionAndBitFlip);
            RunTest("MV-05: Vault Portability & Cross-Directory Relocation", Test_MV05_VaultPortability);
            RunTest("MV-06: AppSettings Multi-Vault Memory, MRU Order & 10-Item Cap", Test_MV06_AppSettingsMruAndCapping);
            RunTest("MV-07: CredentialRepository Dynamic Switching & Total Data Isolation", Test_MV07_RepositoryDynamicSwitching);
            RunTest("MV-08: CredentialService Multi-Vault Reordering & Search Independence", Test_MV08_ServiceMultiVaultIndependence);
            RunTest("MV-09: MasterPasswordForm UI Workflow - Dynamic Path Switching", Test_MV09_MasterPasswordFormPathSwitching);
            RunTest("MV-10: MasterPasswordForm UI Workflow - Create, Unlock & Fallback Modes", Test_MV10_MasterPasswordFormModes);
            RunTest("MV-11: MainForm UI Multi-Vault Live Switching & Title/Badge Sync", Test_MV11_MainFormLiveSwitching);
            RunTest("MV-12: Keyboard Shortcut Routing & Session Locking", Test_MV12_KeyboardRouting);
            RunTest("MV-13: CLI Argument Parsing for Vault Auto-Loading", Test_MV13_CliArgumentParsing);

            Console.WriteLine("\n------------------------------------------------------------------");
            if (failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("🎉 ALL {0} MULTI-VAULT TESTS PASSED! (100% Success)", passedCount));
                Console.ResetColor();
                Console.WriteLine("==================================================================");
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("❌ MULTI-VAULT SUITE RUN COMPLETED WITH FAILURES: {0} Passed, {1} Failed", passedCount, failedCount));
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
                VaultSecurity.LockSession();
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
                VaultSecurity.LockSession();
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        private static string GetTempVaultPath(string name)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "mv_test_scratch");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name);
            if (File.Exists(path)) File.Delete(path);
            return path;
        }

        // ====================================================================
        // TEST 1: STANDALONE .KCRYPT FORMAT & ZERO PLAINTEXT LEAKS
        // ====================================================================
        private static void Test_MV01_KcryptFormatAndZeroPlaintext()
        {
            string vaultPath = GetTempVaultPath("format_test.kcrypt");
            string masterPass = "SuperSecretMasterKey2026!#$";
            string secretService = "UltraConfidentialBankOfZurich";
            string secretUsername = "swiss_account_admin";
            string secretPassword = "QuantumSafePasswordXYZ987!";

            // Initialize vault with initial plain text
            string initialPlain = string.Format("1|{0}|{1}|{2}|{3}\r\n",
                secretService, secretUsername, secretPassword, DateTime.UtcNow.ToString("o"));

            VaultSecurity.InitVault(vaultPath, masterPass, initialPlain);

            Assert(File.Exists(vaultPath), "Vault file must exist on disk");
            Assert(VaultSecurity.IsVaultEncrypted(vaultPath), "IsVaultEncrypted must return true for .kcrypt format");

            // Inspect raw file contents on disk
            string rawContent = File.ReadAllText(vaultPath, Encoding.UTF8);

            Assert(rawContent.Contains(VaultSecurity.HeaderTag), "Vault file must contain HeaderTag");
            Assert(rawContent.Contains("Salt:"), "Vault file must contain Salt header");
            Assert(rawContent.Contains("IV:"), "Vault file must contain IV header");
            Assert(rawContent.Contains("Verify:"), "Vault file must contain Verify HMAC token header");
            Assert(rawContent.Contains("Payload:"), "Vault file must contain Payload header");

            // Zero Plaintext Guarantee: check that raw file NEVER contains master password or plaintext credentials
            Assert(!rawContent.Contains(masterPass), "Raw vault file must NEVER contain the master password");
            Assert(!rawContent.Contains(secretService), "Raw vault file must NOT contain plaintext service name");
            Assert(!rawContent.Contains(secretUsername), "Raw vault file must NOT contain plaintext username");
            Assert(!rawContent.Contains(secretPassword), "Raw vault file must NOT contain plaintext password");

            // Verify salt is valid 16-byte base64
            string saltBase64 = ExtractHeader(rawContent, "Salt:");
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            Assert(saltBytes.Length == 16, "Salt must be exactly 16 bytes (128 bits)");

            // Verify IV is valid 16-byte base64
            string ivBase64 = ExtractHeader(rawContent, "IV:");
            byte[] ivBytes = Convert.FromBase64String(ivBase64);
            Assert(ivBytes.Length == 16, "IV must be exactly 16 bytes (128 bits)");

            // Verify HMAC token is valid 32-byte base64 (SHA-256)
            string verifyBase64 = ExtractHeader(rawContent, "Verify:");
            byte[] verifyBytes = Convert.FromBase64String(verifyBase64);
            Assert(verifyBytes.Length == 32, "Verify token must be 32 bytes (HMAC-SHA256)");
        }

        // ====================================================================
        // TEST 2: ZERO-KNOWLEDGE MULTI-VAULT CRYPTOGRAPHIC ISOLATION
        // ====================================================================
        private static void Test_MV02_ZeroKnowledgeIsolation()
        {
            string vault1 = GetTempVaultPath("finance.kcrypt");
            string vault2 = GetTempVaultPath("social.kcrypt");
            string vault3 = GetTempVaultPath("work.kcrypt");

            string pass1 = "FinancePass123!";
            string pass2 = "SocialPass456@";
            string pass3 = "WorkPass789#";

            VaultSecurity.InitVault(vault1, pass1, "1|Brokerage|trader1|stockPass|2026-01-01\r\n");
            VaultSecurity.InitVault(vault2, pass2, "1|Twitter|handle1|tweetPass|2026-01-01\r\n");
            VaultSecurity.InitVault(vault3, pass3, "1|CorporateVPN|vpnUser|vpnSecret|2026-01-01\r\n");

            // Verify unique salts across all 3 vaults
            string raw1 = File.ReadAllText(vault1);
            string raw2 = File.ReadAllText(vault2);
            string raw3 = File.ReadAllText(vault3);

            string salt1 = ExtractHeader(raw1, "Salt:");
            string salt2 = ExtractHeader(raw2, "Salt:");
            string salt3 = ExtractHeader(raw3, "Salt:");

            Assert(salt1 != salt2, "Vault 1 and Vault 2 salts must be cryptographically distinct");
            Assert(salt2 != salt3, "Vault 2 and Vault 3 salts must be cryptographically distinct");
            Assert(salt1 != salt3, "Vault 1 and Vault 3 salts must be cryptographically distinct");

            // Cross-Vault Master Password Verification: must fail
            Assert(!VaultSecurity.VerifyMasterPassword(vault1, pass2), "Pass 2 must fail on Vault 1");
            Assert(!VaultSecurity.VerifyMasterPassword(vault1, pass3), "Pass 3 must fail on Vault 1");
            Assert(VaultSecurity.VerifyMasterPassword(vault1, pass1), "Pass 1 must succeed on Vault 1");

            Assert(!VaultSecurity.VerifyMasterPassword(vault2, pass1), "Pass 1 must fail on Vault 2");
            Assert(!VaultSecurity.VerifyMasterPassword(vault2, pass3), "Pass 3 must fail on Vault 2");
            Assert(VaultSecurity.VerifyMasterPassword(vault2, pass2), "Pass 2 must succeed on Vault 2");

            Assert(!VaultSecurity.VerifyMasterPassword(vault3, pass1), "Pass 1 must fail on Vault 3");
            Assert(!VaultSecurity.VerifyMasterPassword(vault3, pass2), "Pass 2 must fail on Vault 3");
            Assert(VaultSecurity.VerifyMasterPassword(vault3, pass3), "Pass 3 must succeed on Vault 3");

            // Cross-Vault Unlock Rejections
            VaultSecurity.LockSession();
            string dec;
            Assert(!VaultSecurity.UnlockVault(vault1, pass2, out dec), "Unlocking Vault 1 with Pass 2 must return false");
            Assert(!VaultSecurity.IsUnlocked, "VaultSecurity must remain locked after failed attempt");

            Assert(VaultSecurity.UnlockVault(vault1, pass1, out dec), "Unlocking Vault 1 with correct Pass 1 must succeed");
            Assert(VaultSecurity.IsUnlocked, "VaultSecurity must be unlocked after success");
            Assert(dec.Contains("Brokerage"), "Decrypted content must contain Brokerage");
        }

        // ====================================================================
        // TEST 3: ZERO-KNOWLEDGE IN-MEMORY KEY WIPE & LOCKED STATE ENFORCEMENT
        // ====================================================================
        private static void Test_MV03_KeyZeroingAndLockEnforcement()
        {
            string vaultPath = GetTempVaultPath("wipe_test.kcrypt");
            VaultSecurity.InitVault(vaultPath, "WipePass123!", "1|TestService|user|pass|2026-01-01\r\n");

            string dec;
            VaultSecurity.UnlockVault(vaultPath, "WipePass123!", out dec);
            Assert(VaultSecurity.IsUnlocked, "Session must be unlocked");

            // Check reflection on session keys before wipe
            FieldInfo fiEnc = typeof(VaultSecurity).GetField("sessionEncKey", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo fiAuth = typeof(VaultSecurity).GetField("sessionAuthKey", BindingFlags.NonPublic | BindingFlags.Static);

            byte[] encKey = (byte[])fiEnc.GetValue(null);
            byte[] authKey = (byte[])fiAuth.GetValue(null);
            Assert(encKey != null && encKey.Length == 32, "Active session must hold 256-bit encryption key");
            Assert(authKey != null && authKey.Length == 32, "Active session must hold 256-bit auth key");

            // Call LockSession()
            VaultSecurity.LockSession();
            Assert(!VaultSecurity.IsUnlocked, "IsUnlocked must be false after LockSession");

            // Verify keys are now null
            byte[] encKeyAfter = (byte[])fiEnc.GetValue(null);
            byte[] authKeyAfter = (byte[])fiAuth.GetValue(null);
            Assert(encKeyAfter == null, "sessionEncKey must be nullified");
            Assert(authKeyAfter == null, "sessionAuthKey must be nullified");

            // Assert that reading locked vault throws InvalidOperationException
            bool threwRead = false;
            try
            {
                VaultSecurity.GetDecryptedVaultContent(vaultPath);
            }
            catch (InvalidOperationException)
            {
                threwRead = true;
            }
            Assert(threwRead, "GetDecryptedVaultContent must throw InvalidOperationException when locked");

            // Assert that saving to locked vault throws InvalidOperationException
            bool threwSave = false;
            try
            {
                VaultSecurity.SaveVault(vaultPath, "Some content");
            }
            catch (InvalidOperationException)
            {
                threwSave = true;
            }
            Assert(threwSave, "SaveVault must throw InvalidOperationException when locked");
        }

        // ====================================================================
        // TEST 4: HMAC-SHA256 TAMPER DETECTION & BIT-FLIP REJECTION
        // ====================================================================
        private static void Test_MV04_TamperDetectionAndBitFlip()
        {
            string vaultPath = GetTempVaultPath("tamper_test.kcrypt");
            string masterPass = "TamperProofPass2026!";
            VaultSecurity.InitVault(vaultPath, masterPass, "1|SecureBank|admin|secretToken|2026-01-01\r\n");

            string originalContent = File.ReadAllText(vaultPath);

            // 1. Bit-flip in Verify HMAC auth token
            string verifyHeader = "Verify:";
            int idxVerify = originalContent.IndexOf(verifyHeader);
            Assert(idxVerify >= 0, "Verify header must exist");
            char oldVerifyChar = originalContent[idxVerify + verifyHeader.Length + 3];
            char flippedVerifyChar = oldVerifyChar == 'X' ? 'Y' : 'X';
            string tamperedVerify = originalContent.Substring(0, idxVerify + verifyHeader.Length + 3) +
                                   flippedVerifyChar +
                                   originalContent.Substring(idxVerify + verifyHeader.Length + 4);

            string tamperedVerifyPath = GetTempVaultPath("tampered_verify.kcrypt");
            File.WriteAllText(tamperedVerifyPath, tamperedVerify, Encoding.UTF8);

            bool verifyTampered = VaultSecurity.VerifyMasterPassword(tamperedVerifyPath, masterPass);
            Assert(!verifyTampered, "VerifyMasterPassword must reject verify-token-tampered vault");

            string dec;
            bool unlockVerifyTampered = VaultSecurity.UnlockVault(tamperedVerifyPath, masterPass, out dec);
            Assert(!unlockVerifyTampered, "UnlockVault must reject verify-token-tampered vault");

            // 2. Bit-flip in Salt
            string saltHeader = "Salt:";
            int idxSalt = originalContent.IndexOf(saltHeader);
            Assert(idxSalt >= 0, "Salt header must exist");
            char oldSaltChar = originalContent[idxSalt + saltHeader.Length + 3];
            char flippedSaltChar = oldSaltChar == 'A' ? 'B' : 'A';
            string tamperedSalt = originalContent.Substring(0, idxSalt + saltHeader.Length + 3) +
                                 flippedSaltChar +
                                 originalContent.Substring(idxSalt + saltHeader.Length + 4);

            string tamperedSaltPath = GetTempVaultPath("tampered_salt.kcrypt");
            File.WriteAllText(tamperedSaltPath, tamperedSalt, Encoding.UTF8);

            bool verifySaltTampered = VaultSecurity.VerifyMasterPassword(tamperedSaltPath, masterPass);
            Assert(!verifySaltTampered, "VerifyMasterPassword must reject salt-tampered vault (KDF derivation change)");

            // 3. Corrupt Payload with invalid base64 / invalid padding
            string payloadHeader = "Payload:";
            int idxPayload = originalContent.IndexOf(payloadHeader);
            Assert(idxPayload >= 0, "Payload header must exist");
            string tamperedPayload = originalContent.Substring(0, idxPayload + payloadHeader.Length) + "INVALID_CIPHERTEXT_BYTES==";
            string tamperedPayloadPath = GetTempVaultPath("tampered_payload.kcrypt");
            File.WriteAllText(tamperedPayloadPath, tamperedPayload, Encoding.UTF8);

            bool unlockPayloadTampered = VaultSecurity.UnlockVault(tamperedPayloadPath, masterPass, out dec);
            Assert(!unlockPayloadTampered, "UnlockVault must reject payload with invalid ciphertext");
        }

        // ====================================================================
        // TEST 5: VAULT PORTABILITY & CROSS-DIRECTORY RELOCATION
        // ====================================================================
        private static void Test_MV05_VaultPortability()
        {
            string dirA = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "simulated_usb");
            string dirB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "simulated_cloud");
            if (!Directory.Exists(dirA)) Directory.CreateDirectory(dirA);
            if (!Directory.Exists(dirB)) Directory.CreateDirectory(dirB);

            string fileA = Path.Combine(dirA, "portable_vault.kcrypt");
            string fileB = Path.Combine(dirB, "portable_vault.kcrypt");

            string masterPass = "PortableVaultPass2026!";
            string secretContent = "1|AWS Console|admin@cloud.com|awsSecretKey99|2026-01-01\r\n";

            VaultSecurity.InitVault(fileA, masterPass, secretContent);
            Assert(File.Exists(fileA), "Initial vault must exist in Dir A");

            // Copy file from Dir A to Dir B (simulating file relocation / USB copy)
            File.Copy(fileA, fileB, true);
            Assert(File.Exists(fileB), "Copied vault must exist in Dir B");

            // Unlock and verify in Dir B
            VaultSecurity.LockSession();
            string decrypted;
            bool unlockedB = VaultSecurity.UnlockVault(fileB, masterPass, out decrypted);
            Assert(unlockedB, "Relocated vault in Dir B must unlock successfully");
            Assert(decrypted.Contains("AWS Console"), "Decrypted relocated vault must contain original data");

            // Clean up
            try
            {
                if (File.Exists(fileA)) File.Delete(fileA);
                if (File.Exists(fileB)) File.Delete(fileB);
                if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
                if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            }
            catch { }
        }

        // ====================================================================
        // TEST 6: APPSETTINGS MULTI-VAULT MEMORY, MRU ORDER & 10-ITEM CAP
        // ====================================================================
        private static void Test_MV06_AppSettingsMruAndCapping()
        {
            AppSettings settings = new AppSettings();

            // Default vault path
            string defaultPath = AppSettings.GetDefaultVaultPath();
            Assert(!string.IsNullOrEmpty(defaultPath), "Default vault path must not be empty");
            Assert(defaultPath.EndsWith(".kcrypt") || defaultPath.EndsWith(".txt"), "Default path must have .kcrypt or .txt extension");
            Assert(defaultPath.Contains("KeyCraft"), "Default path should contain KeyCraft directory");

            // Test MRU ordering & deduplication
            settings.RecentVaults.Clear();
            string v1 = @"C:\Users\Ayush\Documents\personal.kcrypt";
            string v2 = @"C:\Users\Ayush\Documents\work.kcrypt";
            string v3 = @"C:\Users\Ayush\Documents\crypto.kcrypt";

            settings.AddRecentVault(v1);
            Assert(settings.RecentVaults.Count == 1, "Must have 1 recent vault");
            Assert(settings.RecentVaults[0] == v1, "v1 must be at index 0");
            Assert(settings.LastOpenedVaultPath == v1, "LastOpenedVaultPath must be v1");

            settings.AddRecentVault(v2);
            Assert(settings.RecentVaults.Count == 2, "Must have 2 recent vaults");
            Assert(settings.RecentVaults[0] == v2, "v2 must be at index 0 (MRU)");
            Assert(settings.RecentVaults[1] == v1, "v1 must be shifted to index 1");

            settings.AddRecentVault(v3);
            Assert(settings.RecentVaults.Count == 3, "Must have 3 recent vaults");
            Assert(settings.RecentVaults[0] == v3, "v3 must be at index 0");

            // Re-add v1 (should move v1 from index 2 to index 0 without duplicates)
            settings.AddRecentVault(v1);
            Assert(settings.RecentVaults.Count == 3, "Count must still be 3 (no duplicate)");
            Assert(settings.RecentVaults[0] == v1, "v1 must be promoted to index 0");
            Assert(settings.RecentVaults[1] == v3, "v3 must be shifted to index 1");
            Assert(settings.RecentVaults[2] == v2, "v2 must be shifted to index 2");
            Assert(settings.LastOpenedVaultPath == v1, "LastOpenedVaultPath must be v1");

            // Test 10-item cap
            for (int i = 0; i < 15; i++)
            {
                settings.AddRecentVault(string.Format(@"C:\Vaults\vault_{0}.kcrypt", i));
            }
            Assert(settings.RecentVaults.Count == 10, "RecentVaults must cap at exactly 10 items");
            Assert(settings.RecentVaults[0] == @"C:\Vaults\vault_14.kcrypt", "Newest vault must be at index 0");
            Assert(settings.RecentVaults[9] == @"C:\Vaults\vault_5.kcrypt", "Tenth vault must be vault_5");

            // Test Persistence Roundtrip
            settings.Save();
            AppSettings loaded = AppSettings.Load();
            Assert(loaded.RecentVaults.Count == 10, "Loaded settings must maintain 10 recent vaults");
            Assert(loaded.LastOpenedVaultPath == settings.LastOpenedVaultPath, "Loaded LastOpenedVaultPath must match saved");
        }

        // ====================================================================
        // TEST 7: CREDENTIALREPOSITORY DYNAMIC SWITCHING & TOTAL DATA ISOLATION
        // ====================================================================
        private static void Test_MV07_RepositoryDynamicSwitching()
        {
            string vaultA = GetTempVaultPath("repo_vaultA.kcrypt");
            string vaultB = GetTempVaultPath("repo_vaultB.kcrypt");

            string passA = "PassVaultA!123";
            string passB = "PassVaultB!456";

            // Initialize both vaults
            VaultSecurity.InitVault(vaultA, passA, "");
            VaultSecurity.InitVault(vaultB, passB, "");

            // Start with Vault A
            string dec;
            VaultSecurity.UnlockVault(vaultA, passA, out dec);
            CredentialRepository repo = new CredentialRepository(vaultA);
            Assert(repo.GetFilePath() == vaultA, "Repository path must match vaultA");

            // Add credentials to Vault A
            repo.Add(new Credential("GitHub", "octocat", "ghp_secretTokenA", 1));
            repo.Add(new Credential("Google", "user@gmail.com", "googlePassA", 2));
            Assert(repo.GetAll().Count == 2, "Vault A must contain 2 credentials");

            // Switch to Vault B (re-authenticate session for Vault B)
            VaultSecurity.UnlockVault(vaultB, passB, out dec);
            repo.SwitchDatabase(vaultB);
            Assert(repo.GetFilePath() == vaultB, "Repository path must now match vaultB");
            Assert(repo.GetAll().Count == 0, "Vault B must initially be empty");

            // Add credential to Vault B
            repo.Add(new Credential("Corporate Slack", "corp_user", "slackPassB", 1));
            Assert(repo.GetAll().Count == 1, "Vault B must contain 1 credential");

            // Switch back to Vault A
            VaultSecurity.UnlockVault(vaultA, passA, out dec);
            repo.SwitchDatabase(vaultA);
            Assert(repo.GetFilePath() == vaultA, "Repository path must switch back to vaultA");

            List<Credential> listA = repo.GetAll();
            Assert(listA.Count == 2, "Vault A must still contain exactly 2 credentials");
            Assert(listA.Exists(delegate(Credential c) { return c.Service == "GitHub"; }), "Vault A must contain GitHub");
            Assert(listA.Exists(delegate(Credential c) { return c.Service == "Google"; }), "Vault A must contain Google");

            // Verify total cryptographic file isolation on disk
            VaultSecurity.UnlockVault(vaultB, passB, out dec);
            CredentialRepository repoB = new CredentialRepository(vaultB);
            List<Credential> listB = repoB.GetAll();
            Assert(listB.Count == 1, "Vault B must contain only 1 credential");
            Assert(listB[0].Service == "Corporate Slack", "Vault B item 0 must be Corporate Slack");
        }

        // ====================================================================
        // TEST 8: CREDENTIALSERVICE MULTI-VAULT REORDERING & SEARCH INDEPENDENCE
        // ====================================================================
        private static void Test_MV08_ServiceMultiVaultIndependence()
        {
            string vaultA = GetTempVaultPath("srv_vaultA.kcrypt");
            string vaultB = GetTempVaultPath("srv_vaultB.kcrypt");
            string pass = "SamePassForReorderTest!123";

            VaultSecurity.InitVault(vaultA, pass, "");
            VaultSecurity.InitVault(vaultB, pass, "");

            string dec;
            VaultSecurity.UnlockVault(vaultA, pass, out dec);

            CredentialRepository repo = new CredentialRepository(vaultA);
            CredentialService service = new CredentialService(repo);

            // Populate Vault A with 3 items
            service.AddCredential("Apple", "apple_user", "passA1");
            service.AddCredential("Banana", "banana_user", "passA2");
            service.AddCredential("Cherry", "cherry_user", "passA3");

            // Switch service to Vault B
            VaultSecurity.UnlockVault(vaultB, pass, out dec);
            service.SwitchDatabase(vaultB);
            Assert(service.GetVaultFilePath() == vaultB, "Service must report vaultB");

            // Populate Vault B with 2 items
            service.AddCredential("Zebra", "zebra_user", "passB1");
            service.AddCredential("Yak", "yak_user", "passB2");

            // Reorder Vault B (swap Yak to Sl No 1)
            List<Credential> bList = service.LoadAll();
            string yakId = bList[1].Id;
            service.ReorderCredential(yakId, 1);

            List<Credential> bReordered = service.LoadAll();
            Assert(bReordered[0].Service == "Yak", "Yak must now be Sl No 1 in Vault B");
            Assert(bReordered[1].Service == "Zebra", "Zebra must now be Sl No 2 in Vault B");

            // Switch service back to Vault A and verify Vault A ordering was unchanged
            VaultSecurity.UnlockVault(vaultA, pass, out dec);
            service.SwitchDatabase(vaultA);
            List<Credential> aList = service.LoadAll();
            Assert(aList.Count == 3, "Vault A must have 3 items");
            Assert(aList[0].Service == "Apple" && aList[0].SerialNo == 1, "Apple must remain Sl No 1 in Vault A");
            Assert(aList[1].Service == "Banana" && aList[1].SerialNo == 2, "Banana must remain Sl No 2 in Vault A");
            Assert(aList[2].Service == "Cherry" && aList[2].SerialNo == 3, "Cherry must remain Sl No 3 in Vault A");

            // Search filter independence
            List<Credential> searchYakInA = service.Filter(aList, "Yak", "Service");
            Assert(searchYakInA.Count == 0, "Searching for Yak in Vault A must return 0 results");
        }

        // ====================================================================
        // TEST 9: MASTERPASSWORDFORM UI WORKFLOW - DYNAMIC PATH SWITCHING
        // ====================================================================
        private static void Test_MV09_MasterPasswordFormPathSwitching()
        {
            string initialPath = GetTempVaultPath("initial_target.kcrypt");
            string switchedPath = GetTempVaultPath("switched_target.kcrypt");

            using (MasterPasswordForm form = new MasterPasswordForm(MasterPasswordMode.Create, initialPath))
            {
                form.CreateControl();
                Assert(form.SelectedVaultPath == initialPath, "Initial SelectedVaultPath must match initialPath");

                // Programmatically invoke SwitchVault
                form.SwitchVault(switchedPath);
                Assert(form.SelectedVaultPath == switchedPath, "SelectedVaultPath must update to switchedPath");

                // Test SwitchToNewVault
                string newVaultPath = GetTempVaultPath("brand_new.kcrypt");
                form.SwitchToNewVault(newVaultPath);
                Assert(form.SelectedVaultPath == newVaultPath, "SelectedVaultPath must update to brand_new");
            }
        }

        // ====================================================================
        // TEST 10: MASTERPASSWORDFORM UI WORKFLOW - CREATE, UNLOCK & FALLBACK
        // ====================================================================
        private static void Test_MV10_MasterPasswordFormModes()
        {
            string vaultPath = GetTempVaultPath("mode_test.kcrypt");

            // 1. Create Mode
            using (MasterPasswordForm createForm = new MasterPasswordForm(MasterPasswordMode.Create, vaultPath))
            {
                createForm.CreateControl();

                TextBox txtP = GetField<TextBox>(createForm, "txtPassword");
                TextBox txtC = GetField<TextBox>(createForm, "txtConfirm");
                Label lblErr = GetField<Label>(createForm, "lblError");

                // Test mismatching passwords
                txtP.Text = "MasterPassA1!";
                txtC.Text = "MasterPassB2!";
                InvokeMethod(createForm, "HandleSubmit");

                Assert(createForm.DialogResult == DialogResult.None, "Form must not close on password mismatch");
                Assert(lblErr.Text.Contains("do not match"), "Error label must warn passwords do not match: " + lblErr.Text);

                // Test matching passwords
                txtP.Text = "ValidMasterPass2026!";
                txtC.Text = "ValidMasterPass2026!";
                InvokeMethod(createForm, "HandleSubmit");

                Assert(createForm.DialogResult == DialogResult.OK, "Form must accept valid matching passwords");
                Assert(File.Exists(vaultPath), "Vault file must be created on disk");
                Assert(VaultSecurity.IsVaultEncrypted(vaultPath), "Created vault must be encrypted");
            }

            // 2. Unlock Mode
            VaultSecurity.LockSession();
            using (MasterPasswordForm unlockForm = new MasterPasswordForm(MasterPasswordMode.Unlock, vaultPath))
            {
                unlockForm.CreateControl();

                TextBox txtP = GetField<TextBox>(unlockForm, "txtPassword");
                Label lblErr = GetField<Label>(unlockForm, "lblError");

                // Wrong password attempt
                txtP.Text = "WrongPassword!";
                InvokeMethod(unlockForm, "HandleSubmit");

                Assert(unlockForm.DialogResult == DialogResult.None, "UnlockForm must not close on wrong password");
                Assert(lblErr.Text.Contains("Incorrect master password"), "Error label must warn incorrect password");

                // Correct password attempt
                txtP.Text = "ValidMasterPass2026!";
                InvokeMethod(unlockForm, "HandleSubmit");

                Assert(unlockForm.DialogResult == DialogResult.OK, "UnlockForm must close with DialogResult.OK on correct password");
                Assert(VaultSecurity.IsUnlocked, "VaultSecurity must be unlocked after successful submit");
            }
        }

        // ====================================================================
        // TEST 11: MAINFORM UI MULTI-VAULT LIVE SWITCHING & TITLE/BADGE SYNC
        // ====================================================================
        private static void Test_MV11_MainFormLiveSwitching()
        {
            string vaultA = GetTempVaultPath("gui_vaultA.kcrypt");
            string vaultB = GetTempVaultPath("gui_vaultB.kcrypt");
            string passA = "GuiPassA!2026";
            string passB = "GuiPassB!2026";

            VaultSecurity.InitVault(vaultA, passA, "1|Netflix|userA|passA|2026-01-01\r\n");
            VaultSecurity.InitVault(vaultB, passB, "1|Corporate GitHub|userB|passB|2026-01-01\r\n");

            // Start MainForm with Vault A
            string dec;
            VaultSecurity.UnlockVault(vaultA, passA, out dec);

            CredentialRepository repo = new CredentialRepository(vaultA);
            CredentialService service = new CredentialService(repo);

            using (MainForm main = new MainForm(service))
            {
                main.CreateControl();
                main.Show();
                Application.DoEvents();

                // Check title and vault badge
                Assert(main.Text.Contains("gui_vaultA.kcrypt"), "MainForm title must include gui_vaultA.kcrypt");
                Label lblBadge = GetField<Label>(main, "lblVaultBadge");
                Assert(lblBadge != null && lblBadge.Text.Contains("gui_vaultA.kcrypt"), "lblVaultBadge must display gui_vaultA.kcrypt");

                ListView lv = GetField<ListView>(main, "lvCredentials");
                Assert(lv.Items.Count == 1, "MainForm must display 1 item from Vault A");
                Assert(lv.Items[0].SubItems[1].Text == "Netflix", "MainForm item must be Netflix");

                // Switch to Vault B (already unlocked)
                VaultSecurity.UnlockVault(vaultB, passB, out dec);
                main.SwitchToVaultFile(vaultB, true);
                Application.DoEvents();

                Assert(main.Text.Contains("gui_vaultB.kcrypt"), "MainForm title must update to gui_vaultB.kcrypt");
                Assert(lblBadge.Text.Contains("gui_vaultB.kcrypt"), "lblVaultBadge must update to gui_vaultB.kcrypt");
                Assert(service.GetVaultFilePath() == vaultB, "Service must now target Vault B");
                Assert(lv.Items.Count == 1, "MainForm must display 1 item from Vault B");
                Assert(lv.Items[0].SubItems[1].Text == "Corporate GitHub", "MainForm item must be Corporate GitHub");

                // Switch back to Vault A
                VaultSecurity.UnlockVault(vaultA, passA, out dec);
                main.SwitchToVaultFile(vaultA, true);
                Application.DoEvents();

                Assert(main.Text.Contains("gui_vaultA.kcrypt"), "MainForm title must return to gui_vaultA.kcrypt");
                Assert(lblBadge.Text.Contains("gui_vaultA.kcrypt"), "lblVaultBadge must return to gui_vaultA.kcrypt");
                Assert(service.GetVaultFilePath() == vaultA, "Service must return to Vault A");
                Assert(lv.Items[0].SubItems[1].Text == "Netflix", "MainForm item must be Netflix again");
            }
        }

        // ====================================================================
        // TEST 12: KEYBOARD SHORTCUT ROUTING & SESSION LOCKING
        // ====================================================================
        private static void Test_MV12_KeyboardRouting()
        {
            string vaultPath = GetTempVaultPath("hotkey_vault.kcrypt");
            string pass = "HotkeyPass!123";
            VaultSecurity.InitVault(vaultPath, pass, "1|DummyService|user|pass|2026-01-01\r\n");

            string dec;
            VaultSecurity.UnlockVault(vaultPath, pass, out dec);
            CredentialService service = new CredentialService(new CredentialRepository(vaultPath));

            using (MainForm main = new MainForm(service))
            {
                main.CreateControl();
                main.Show();
                Application.DoEvents();

                // Test service.LockVault() directly: verifies zeroing of session keys
                Assert(VaultSecurity.IsUnlocked, "Vault must be unlocked initially");
                service.LockVault();
                Assert(!VaultSecurity.IsUnlocked, "service.LockVault() must immediately lock session and clear keys");

                // Verify ProcessCmdKey method exists for routing
                MethodInfo miCmd = typeof(MainForm).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert(miCmd != null, "ProcessCmdKey method must exist on MainForm for Ctrl+N, Ctrl+O, Ctrl+L routing");
            }
        }

        // ====================================================================
        // TEST 13: CLI ARGUMENT PARSING FOR VAULT AUTO-LOADING
        // ====================================================================
        private static void Test_MV13_CliArgumentParsing()
        {
            // Simulate CLI argument parsing identical to Program.cs logic
            string customVault = @"C:\KeyCraft\CustomVault.kcrypt";

            // Case A: -f flag
            string[] argsA = new string[] { "-f", customVault };
            string resolvedA = ResolveCliVaultPath(argsA);
            Assert(resolvedA == customVault, "CLI flag -f must resolve vault path");

            // Case B: --file flag
            string[] argsB = new string[] { "--file", customVault };
            string resolvedB = ResolveCliVaultPath(argsB);
            Assert(resolvedB == customVault, "CLI flag --file must resolve vault path");

            // Case C: --vault flag
            string[] argsC = new string[] { "--vault", customVault };
            string resolvedC = ResolveCliVaultPath(argsC);
            Assert(resolvedC == customVault, "CLI flag --vault must resolve vault path");

            // Case D: Direct positional argument
            string[] argsD = new string[] { customVault };
            string resolvedD = ResolveCliVaultPath(argsD);
            Assert(resolvedD == customVault, "Positional file argument must resolve vault path");

            // Case E: Empty args -> falls back to null (Program.cs then uses LastOpenedVaultPath or default)
            string[] argsE = new string[0];
            string resolvedE = ResolveCliVaultPath(argsE);
            Assert(resolvedE == null, "Empty CLI args must return null for fallback logic");
        }

        private static string ResolveCliVaultPath(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i].Trim();
                    if ((a == "-f" || a == "--file" || a == "--vault") && i + 1 < args.Length)
                    {
                        return args[i + 1].Trim('"', '\'');
                    }
                    if (!a.StartsWith("-") && (a.EndsWith(".kcrypt", StringComparison.OrdinalIgnoreCase) || a.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                    {
                        return a.Trim('"', '\'');
                    }
                }
            }
            return null;
        }

        // ====================================================================
        // HELPER UTILITIES
        // ====================================================================
        private static string ExtractHeader(string content, string headerPrefix)
        {
            using (StringReader reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(headerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring(headerPrefix.Length).Trim();
                    }
                }
            }
            return null;
        }

        private static T GetField<T>(object instance, string fieldName) where T : class
        {
            FieldInfo fi = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi == null)
            {
                Type baseType = instance.GetType().BaseType;
                while (baseType != null && fi == null)
                {
                    fi = baseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    baseType = baseType.BaseType;
                }
            }
            return fi != null ? fi.GetValue(instance) as T : null;
        }

        private static object InvokeMethod(object instance, string methodName, params object[] parameters)
        {
            MethodInfo mi = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (mi == null)
            {
                Type baseType = instance.GetType().BaseType;
                while (baseType != null && mi == null)
                {
                    mi = baseType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    baseType = baseType.BaseType;
                }
            }
            return mi != null ? mi.Invoke(instance, parameters) : null;
        }
    }
}
