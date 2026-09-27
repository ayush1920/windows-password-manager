using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGui
{
    /// <summary>
    /// Core cryptographic security engine for KeyCraft.
    /// Provides AES-256-CBC encryption, PBKDF2 key derivation (100,000 iterations),
    /// HMAC-SHA256 integrity verification, and active session management.
    /// </summary>
    public static class VaultSecurity
    {
        public const string HeaderTag = "KEYCRAFT_ENC_V1";
        private const int SaltBytes = 16;
        private const int KeyBytes = 32; // AES-256 (256 bits)
        private const int IvBytes = 16;  // AES Block size
        private const int Iterations = 100000;
        private const string AuthPayload = "KEYCRAFT_VAULT_INTEGRITY_TOKEN_V1";

        // Active Session Keys (in-memory only, wiped on lock)
        private static byte[] sessionEncKey = null;
        private static byte[] sessionAuthKey = null;
        private static byte[] sessionSalt = null;
        private static bool isUnlocked = false;

        public static bool IsUnlocked
        {
            get { return isUnlocked; }
        }

        /// <summary>
        /// Checks whether a given database file exists and is in the encrypted vault format.
        /// </summary>
        public static bool IsVaultEncrypted(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed))
                        {
                            continue;
                        }
                        return string.Equals(trimmed, HeaderTag, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Derives a 32-byte AES encryption key and a 32-byte HMAC authentication key
        /// from the master password and salt using PBKDF2 (100,000 rounds).
        /// </summary>
        public static void DeriveKeys(string masterPassword, byte[] salt, out byte[] encKey, out byte[] authKey)
        {
            if (string.IsNullOrEmpty(masterPassword))
            {
                throw new ArgumentException("Master password cannot be empty.", "masterPassword");
            }
            if (salt == null || salt.Length < SaltBytes)
            {
                throw new ArgumentException("Invalid salt.", "salt");
            }

            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, Iterations))
            {
                encKey = pbkdf2.GetBytes(KeyBytes);
                authKey = pbkdf2.GetBytes(KeyBytes);
            }
        }

        /// <summary>
        /// Computes the HMAC-SHA256 verification token for verifying master passwords.
        /// </summary>
        public static string ComputeAuthToken(byte[] authKey, byte[] salt)
        {
            using (HMACSHA256 hmac = new HMACSHA256(authKey))
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(AuthPayload);
                byte[] combined = new byte[salt.Length + payloadBytes.Length];
                Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
                Buffer.BlockCopy(payloadBytes, 0, combined, salt.Length, payloadBytes.Length);
                byte[] hash = hmac.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Compares two strings in constant time to prevent timing side-channel attacks.
        /// </summary>
        public static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);
            if (aBytes.Length != bBytes.Length) return false;

            int diff = 0;
            for (int i = 0; i < aBytes.Length; i++)
            {
                diff |= (aBytes[i] ^ bBytes[i]);
            }
            return diff == 0;
        }

        /// <summary>
        /// Encrypts plain text using AES-256-CBC with PKCS7 padding.
        /// </summary>
        public static string EncryptString(string plainText, byte[] encKey, out byte[] iv)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                iv = new byte[IvBytes];
                rng.GetBytes(iv);
            }

            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encKey;
                aes.IV = iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts AES-256-CBC ciphertext into plain text.
        /// </summary>
        public static string DecryptString(string cipherTextBase64, byte[] encKey, byte[] iv)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encKey;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream ms = new MemoryStream(cipherBytes))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Initializes and encrypts a database vault with a newly chosen master password.
        /// Can take existing plain-text content to seamlessly migrate it into the vault.
        /// </summary>
        public static void InitializeAndEncryptVault(string filePath, string masterPassword, string initialPlainText)
        {
            byte[] salt = new byte[SaltBytes];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] encKey, authKey;
            DeriveKeys(masterPassword, salt, out encKey, out authKey);

            byte[] iv;
            string cipherText = EncryptString(initialPlainText ?? string.Empty, encKey, out iv);
            string verifyToken = ComputeAuthToken(authKey, salt);

            WriteVaultFile(filePath, salt, iv, verifyToken, cipherText);

            // Establish active in-memory session
            SetSession(encKey, authKey, salt);
        }

        /// <summary>
        /// Shorthand helper to initialize and encrypt a vault.
        /// </summary>
        public static void InitVault(string filePath, string masterPassword, string initialPlainText = "")
        {
            InitializeAndEncryptVault(filePath, masterPassword, initialPlainText);
        }

        /// <summary>
        /// Verifies the master password and unlocks the database vault.
        /// </summary>
        public static bool UnlockVault(string filePath, string masterPassword, out string decryptedPlainText)
        {
            decryptedPlainText = null;
            if (!File.Exists(filePath)) return false;

            byte[] salt, iv;
            string verifyToken, cipherText;
            if (!ReadVaultFile(filePath, out salt, out iv, out verifyToken, out cipherText))
            {
                return false;
            }

            byte[] encKey, authKey;
            DeriveKeys(masterPassword, salt, out encKey, out authKey);

            string expectedToken = ComputeAuthToken(authKey, salt);
            if (!ConstantTimeEquals(expectedToken, verifyToken))
            {
                return false; // Invalid master password
            }

            try
            {
                decryptedPlainText = DecryptString(cipherText, encKey, iv);
                SetSession(encKey, authKey, salt);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reads and decrypts the vault content using the active authenticated session.
        /// </summary>
        public static string GetDecryptedVaultContent(string filePath)
        {
            if (!isUnlocked || sessionEncKey == null)
            {
                throw new InvalidOperationException("Vault is locked. Master password required to read credentials.");
            }

            byte[] salt, iv;
            string verifyToken, cipherText;
            if (!ReadVaultFile(filePath, out salt, out iv, out verifyToken, out cipherText))
            {
                throw new InvalidOperationException("Failed to read encrypted vault file.");
            }

            return DecryptString(cipherText, sessionEncKey, iv);
        }

        /// <summary>
        /// Verifies whether a given master password is valid for the vault.
        /// </summary>
        public static bool VerifyMasterPassword(string filePath, string masterPassword)
        {
            if (!File.Exists(filePath)) return false;

            byte[] salt, iv;
            string verifyToken, cipherText;
            if (!ReadVaultFile(filePath, out salt, out iv, out verifyToken, out cipherText))
            {
                return false;
            }

            byte[] encKey, authKey;
            DeriveKeys(masterPassword, salt, out encKey, out authKey);

            string expectedToken = ComputeAuthToken(authKey, salt);
            return ConstantTimeEquals(expectedToken, verifyToken);
        }

        /// <summary>
        /// Re-encrypts the vault file using the active session keys.
        /// </summary>
        public static void SaveVault(string filePath, string plainText)
        {
            if (!isUnlocked || sessionEncKey == null || sessionAuthKey == null || sessionSalt == null)
            {
                throw new InvalidOperationException("Vault is locked. Cannot save encrypted data.");
            }

            byte[] iv;
            string cipherText = EncryptString(plainText ?? string.Empty, sessionEncKey, out iv);
            string verifyToken = ComputeAuthToken(sessionAuthKey, sessionSalt);

            WriteVaultFile(filePath, sessionSalt, iv, verifyToken, cipherText);
        }

        /// <summary>
        /// Changes the master password, re-derives new encryption keys, and re-encrypts the vault.
        /// </summary>
        public static bool ChangeMasterPassword(string filePath, string currentPassword, string newPassword, string currentPlainText)
        {
            if (!VerifyMasterPassword(filePath, currentPassword))
            {
                return false;
            }

            InitializeAndEncryptVault(filePath, newPassword, currentPlainText);
            return true;
        }

        /// <summary>
        /// Sets active session keys in memory.
        /// </summary>
        public static void SetSession(byte[] encKey, byte[] authKey, byte[] salt)
        {
            sessionEncKey = encKey;
            sessionAuthKey = authKey;
            sessionSalt = salt;
            isUnlocked = true;
        }

        /// <summary>
        /// Wipes encryption keys from memory and locks the session.
        /// </summary>
        public static void LockSession()
        {
            if (sessionEncKey != null)
            {
                Array.Clear(sessionEncKey, 0, sessionEncKey.Length);
                sessionEncKey = null;
            }
            if (sessionAuthKey != null)
            {
                Array.Clear(sessionAuthKey, 0, sessionAuthKey.Length);
                sessionAuthKey = null;
            }
            if (sessionSalt != null)
            {
                Array.Clear(sessionSalt, 0, sessionSalt.Length);
                sessionSalt = null;
            }
            isUnlocked = false;
        }

        private static void WriteVaultFile(string filePath, byte[] salt, byte[] iv, string verifyToken, string cipherText)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# ========================================================");
            sb.AppendLine("# KeyCraft Encrypted Credential Vault");
            sb.AppendLine("# Format: KEYCRAFT_ENC_V1");
            sb.AppendLine("# Cipher: AES-256-CBC | KDF: PBKDF2 (100,000 iterations)");
            sb.AppendLine("# Protected by Master Password — Do not modify manually");
            sb.AppendLine("# ========================================================");
            sb.AppendLine(HeaderTag);
            sb.AppendLine("Salt:" + Convert.ToBase64String(salt));
            sb.AppendLine("IV:" + Convert.ToBase64String(iv));
            sb.AppendLine("Verify:" + verifyToken);
            sb.AppendLine("Payload:" + cipherText);
            SafeFileStorage.WriteAllTextAtomic(filePath, sb.ToString(), true);
        }

        private static bool ReadVaultFile(string filePath, out byte[] salt, out byte[] iv, out string verifyToken, out string cipherText)
        {
            salt = null;
            iv = null;
            verifyToken = null;
            cipherText = null;

            if (!File.Exists(filePath)) return false;

            try
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                bool headerFound = false;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                    if (!headerFound)
                    {
                        if (string.Equals(trimmed, HeaderTag, StringComparison.OrdinalIgnoreCase))
                        {
                            headerFound = true;
                        }
                        continue;
                    }

                    if (trimmed.StartsWith("Salt:", StringComparison.OrdinalIgnoreCase))
                    {
                        salt = Convert.FromBase64String(trimmed.Substring(5).Trim());
                    }
                    else if (trimmed.StartsWith("IV:", StringComparison.OrdinalIgnoreCase))
                    {
                        iv = Convert.FromBase64String(trimmed.Substring(3).Trim());
                    }
                    else if (trimmed.StartsWith("Verify:", StringComparison.OrdinalIgnoreCase))
                    {
                        verifyToken = trimmed.Substring(7).Trim();
                    }
                    else if (trimmed.StartsWith("Payload:", StringComparison.OrdinalIgnoreCase))
                    {
                        cipherText = trimmed.Substring(8).Trim();
                    }
                }

                return headerFound && salt != null && iv != null && !string.IsNullOrEmpty(verifyToken) && cipherText != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
