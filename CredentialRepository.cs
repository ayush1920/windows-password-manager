using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGui
{
    /// <summary>
    /// Database / storage layer for reading and persisting credentials in a plain text file.
    /// Also manages master password hash and vault settings.
    /// </summary>
    public class CredentialRepository
    {
        private string filePath;
        private string settingsPath;
        private bool hasActiveVault;
        private const string Delimiter = "|";
        private const string PipeEscape = "%%PIPE%%";

        public bool HasActiveVault
        {
            get { return hasActiveVault && !string.IsNullOrEmpty(filePath); }
        }

        public CredentialRepository(string customPath = null, bool isDeferredOrEmpty = false)
        {
            if (isDeferredOrEmpty || string.IsNullOrEmpty(customPath))
            {
                filePath = null;
                settingsPath = null;
                hasActiveVault = false;
                return;
            }

            filePath = Path.GetFullPath(customPath);
            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir)) dir = ".";
            settingsPath = Path.Combine(dir, "vault_settings.txt");
            hasActiveVault = true;

            EnsureFileInitialized();
        }

        public void SwitchDatabase(string newPath)
        {
            if (string.IsNullOrEmpty(newPath))
            {
                filePath = null;
                settingsPath = null;
                hasActiveVault = false;
                return;
            }

            filePath = Path.GetFullPath(newPath);
            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir)) dir = ".";
            settingsPath = Path.Combine(dir, "vault_settings.txt");
            hasActiveVault = true;

            EnsureFileInitialized();
        }

        public string GetFilePath()
        {
            return filePath;
        }

        public static string GetDefaultPath()
        {
            return AppSettings.GetDefaultVaultPath();
        }


        public string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string[] lines = File.ReadAllLines(settingsPath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;
                        int idx = trimmed.IndexOf('=');
                        string k = trimmed.Substring(0, idx).Trim();
                        string v = trimmed.Substring(idx + 1).Trim();
                        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        {
                            return v;
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(settingsPath))
                {
                    string[] lines = File.ReadAllLines(settingsPath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;
                        int idx = trimmed.IndexOf('=');
                        string k = trimmed.Substring(0, idx).Trim();
                        string v = trimmed.Substring(idx + 1).Trim();
                        dict[k] = v;
                    }
                }

                dict[key] = value;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# KeyCraft Vault Settings");
                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    sb.AppendLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                }
                File.WriteAllText(settingsPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving setting: " + ex.Message);
            }
        }

        public bool HasMasterPassword()
        {
            if (HasActiveVault && !string.IsNullOrEmpty(filePath) && VaultSecurity.IsVaultEncrypted(filePath))
            {
                return true;
            }
            string storedHash = GetSetting("MasterPasswordHash", "");
            return !string.IsNullOrEmpty(storedHash);
        }

        public bool VerifyMasterPassword(string input)
        {
            if (input == null) return false;
            if (HasActiveVault && !string.IsNullOrEmpty(filePath) && VaultSecurity.IsVaultEncrypted(filePath))
            {
                string decrypted;
                return VaultSecurity.UnlockVault(filePath, input, out decrypted);
            }

            string storedHash = GetSetting("MasterPasswordHash", "");
            if (string.IsNullOrEmpty(storedHash))
            {
                // No master password set: empty input matches (unlocked without password)
                return string.IsNullOrEmpty(input);
            }

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            string inputHash = HashPassword(input);
            return string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase);
        }

        public void SetMasterPassword(string newPassword)
        {
            string hash = HashPassword(newPassword ?? "");
            SetSetting("MasterPasswordHash", hash);
        }

        public bool ChangeMasterPassword(string currentPassword, string newPassword, out string error)
        {
            if (!HasActiveVault || string.IsNullOrEmpty(filePath))
            {
                error = "No vault file is currently loaded. Open or create a vault first.";
                return false;
            }

            if (VaultSecurity.IsVaultEncrypted(filePath))
            {
                string decrypted;
                if (!VaultSecurity.UnlockVault(filePath, currentPassword, out decrypted))
                {
                    error = "Current master password is incorrect.";
                    return false;
                }

                try
                {
                    bool ok = VaultSecurity.ChangeMasterPassword(filePath, currentPassword, newPassword, decrypted ?? string.Empty);
                    if (!ok)
                    {
                        error = "Failed to re-encrypt vault file with new master password.";
                        return false;
                    }
                    error = null;
                    return true;
                }
                catch (Exception ex)
                {
                    error = "Encryption error: " + ex.Message;
                    return false;
                }
            }
            else
            {
                if (HasMasterPassword() && !VerifyMasterPassword(currentPassword))
                {
                    error = "Current master password is incorrect.";
                    return false;
                }
                SetMasterPassword(newPassword);
                error = null;
                return true;
            }
        }

        private string HashPassword(string pwd)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(pwd + "__keycraft_salt_2026__");
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private void EnsureFileInitialized()
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return;
                if (filePath.EndsWith(".kcrypt", StringComparison.OrdinalIgnoreCase)) return;

                if (!File.Exists(filePath))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# ========================================================");
                    sb.AppendLine("# KeyCraft Plain-Text Credential Store (Windows 11 Fluent)");
                    sb.AppendLine("# Format: Id|Service|Username|Password|LastUpdated|Notes");
                    sb.AppendLine("# ========================================================");

                    SafeFileStorage.WriteAllTextAtomic(filePath, sb.ToString(), false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error initializing credential file: " + ex.Message);
            }
        }

        public List<Credential> GetAll()
        {
            List<Credential> list = new List<Credential>();
            if (!HasActiveVault || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return list;
            }

            try
            {
                string[] lines;
                if (VaultSecurity.IsVaultEncrypted(filePath))
                {
                    string content = VaultSecurity.GetDecryptedVaultContent(filePath);
                    lines = (content ?? string.Empty).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                }
                else
                {
                    lines = File.ReadAllLines(filePath, Encoding.UTF8);
                }

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue;

                    Credential cred = DeserializeLine(trimmed, list.Count + 1);
                    if (cred != null)
                    {
                        list.Add(cred);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read credentials file: " + ex.Message, ex);
            }

            return list;
        }

        public void Add(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException("cred");
            List<Credential> list = GetAll();
            cred.LastUpdated = DateTime.Now;
            cred.SerialNo = list.Count + 1;
            list.Add(cred);
            SaveAll(list);
        }

        public bool Update(Credential updatedCred)
        {
            if (updatedCred == null) throw new ArgumentNullException("updatedCred");
            List<Credential> list = GetAll();
            int index = list.FindIndex(delegate (Credential c) { return c.Id == updatedCred.Id; });
            if (index >= 0)
            {
                updatedCred.LastUpdated = DateTime.Now;
                if (updatedCred.SerialNo <= 0) updatedCred.SerialNo = list[index].SerialNo;
                list[index] = updatedCred;
                SaveAll(list);
                return true;
            }
            return false;
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            List<Credential> list = GetAll();
            int countBefore = list.Count;
            list.RemoveAll(delegate (Credential c) { return c.Id == id; });
            if (list.Count < countBefore)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SerialNo = i + 1;
                }
                SaveAll(list);
                return true;
            }
            return false;
        }

        public void SaveAll(List<Credential> list)
        {
            if (!HasActiveVault || string.IsNullOrEmpty(filePath))
            {
                throw new InvalidOperationException("No vault file is currently loaded.");
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# ========================================================");
                sb.AppendLine("# KeyCraft Plain-Text Credential Store (Windows 11 Fluent)");
                sb.AppendLine("# Format: Id|Service|Username|Password|LastUpdated|Notes");
                sb.AppendLine("# ========================================================");

                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Credential cred = list[i];
                        if (cred != null)
                        {
                            cred.SerialNo = i + 1;
                            sb.AppendLine(SerializeLine(cred));
                        }
                    }
                }

                if (VaultSecurity.IsVaultEncrypted(filePath))
                {
                    VaultSecurity.SaveVault(filePath, sb.ToString());
                }
                else
                {
                    SafeFileStorage.WriteAllTextAtomic(filePath, sb.ToString(), true);
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to write credentials file: " + ex.Message, ex);
            }
        }

        public static string SerializeLine(Credential c)
        {
            if (c == null) return string.Empty;
            string id = Escape(c.Id);
            string service = Escape(c.Service);
            string username = Escape(c.Username);
            string password = Escape(c.Password);
            string updated = c.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
            string notes = Escape(c.Notes ?? "");

            return string.Format("{0}{1}{2}{1}{3}{1}{4}{1}{5}{1}{6}", id, Delimiter, service, username, password, updated, notes);
        }

        public static Credential DeserializeLine(string line, int serialNo = 0)
        {
            if (string.IsNullOrEmpty(line)) return null;
            string[] parts = line.Split(new string[] { Delimiter }, StringSplitOptions.None);
            if (parts.Length >= 5)
            {
                Credential c = new Credential();
                c.Id = Unescape(parts[0]);
                c.SerialNo = serialNo;
                c.Service = Unescape(parts[1]);
                c.Username = Unescape(parts[2]);
                c.Password = Unescape(parts[3]);

                DateTime dt;
                if (DateTime.TryParse(parts[4], out dt))
                {
                    c.LastUpdated = dt;
                }
                else
                {
                    c.LastUpdated = DateTime.Now;
                }

                if (parts.Length >= 6)
                {
                    c.Notes = Unescape(parts[5]);
                }
                else
                {
                    c.Notes = string.Empty;
                }

                return c;
            }
            return null;
        }

        private static string Escape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(Delimiter, PipeEscape).Replace("\r", "").Replace("\n", " ");
        }

        private static string Unescape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(PipeEscape, Delimiter);
        }
    }
}
