using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PasswordGui
{
    /// <summary>
    /// Database / storage layer for reading and persisting credentials in a plain text file.
    /// </summary>
    public class CredentialRepository
    {
        private readonly string filePath;
        private const string Delimiter = "|";
        private const string PipeEscape = "%%PIPE%%";

        public static string GetDefaultPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string parentDataDir = Path.Combine(baseDir, "..", "data");
            string localDataDir = Path.Combine(baseDir, "data");

            string targetDir;
            if (Directory.Exists(parentDataDir))
            {
                targetDir = Path.GetFullPath(parentDataDir);
            }
            else if (Directory.Exists(localDataDir))
            {
                targetDir = Path.GetFullPath(localDataDir);
            }
            else
            {
                string dirName = new DirectoryInfo(baseDir).Name;
                if (string.Equals(dirName, "bin", StringComparison.OrdinalIgnoreCase))
                {
                    targetDir = Path.GetFullPath(parentDataDir);
                }
                else
                {
                    targetDir = Path.GetFullPath(localDataDir);
                }
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            return Path.Combine(targetDir, "credentials.txt");
        }

        public CredentialRepository(string customPath = null)
        {
            filePath = string.IsNullOrEmpty(customPath) ? GetDefaultPath() : customPath;
        }

        public string GetFilePath()
        {
            return filePath;
        }

        /// <summary>
        /// Retrieves all credentials stored in the database, sorted by serial number.
        /// Decrypts from the encrypted vault if protected by a master password.
        /// </summary>
        public List<Credential> GetAll()
        {
            List<Credential> list = new List<Credential>();
            if (!File.Exists(filePath))
            {
                return list;
            }

            try
            {
                string content;
                if (VaultSecurity.IsVaultEncrypted(filePath))
                {
                    content = VaultSecurity.GetDecryptedVaultContent(filePath);
                }
                else
                {
                    content = File.ReadAllText(filePath, Encoding.UTF8);
                }

                string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                int lineSerial = 1;
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue; // Comment line
                    if (string.Equals(trimmed, VaultSecurity.HeaderTag, StringComparison.OrdinalIgnoreCase)) continue;
                    if (trimmed.StartsWith("Salt:", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("IV:", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("Verify:", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("Payload:", StringComparison.OrdinalIgnoreCase)) continue;

                    Credential cred = DeserializeLine(trimmed, lineSerial);
                    if (cred != null)
                    {
                        list.Add(cred);
                        lineSerial++;
                    }
                }

                // Keep ordered by serial number
                list.Sort(delegate(Credential a, Credential b) { return a.SerialNo.CompareTo(b.SerialNo); });

                // Ensure continuous 1..N serial numbers
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SerialNo = i + 1;
                }
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read credentials: " + ex.Message, ex);
            }

            return list;
        }

        /// <summary>
        /// Adds a new credential and saves to the text file.
        /// </summary>
        public void Add(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException("cred");
            List<Credential> list = GetAll();
            cred.LastUpdated = DateTime.Now;

            if (cred.SerialNo <= 0 || cred.SerialNo > list.Count + 1)
            {
                cred.SerialNo = list.Count + 1;
            }

            int targetIdx = cred.SerialNo - 1;
            if (targetIdx >= list.Count)
            {
                list.Add(cred);
            }
            else
            {
                list.Insert(targetIdx, cred);
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].SerialNo = i + 1;
            }

            SaveAll(list);
        }

        /// <summary>
        /// Updates an existing credential matched by Id, with optional targetSerialNo remapping.
        /// </summary>
        public bool Update(Credential updatedCred, int targetSerialNo = 0)
        {
            if (updatedCred == null) throw new ArgumentNullException("updatedCred");
            List<Credential> list = GetAll();
            int index = list.FindIndex(delegate(Credential c) { return c.Id == updatedCred.Id; });
            if (index >= 0)
            {
                updatedCred.LastUpdated = DateTime.Now;
                list[index] = updatedCred;

                if (targetSerialNo > 0 && targetSerialNo != (index + 1))
                {
                    Credential item = list[index];
                    list.RemoveAt(index);
                    int targetIdx = targetSerialNo - 1;
                    if (targetIdx < 0) targetIdx = 0;
                    if (targetIdx > list.Count) targetIdx = list.Count;
                    list.Insert(targetIdx, item);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SerialNo = i + 1;
                }

                SaveAll(list);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remaps / moves a credential to a new serial number position.
        /// </summary>
        public bool Reorder(string id, int targetSerialNo)
        {
            if (string.IsNullOrEmpty(id) || targetSerialNo <= 0) return false;
            List<Credential> list = GetAll();
            int currentIndex = list.FindIndex(delegate(Credential c) { return c.Id == id; });
            if (currentIndex >= 0)
            {
                Credential item = list[currentIndex];
                list.RemoveAt(currentIndex);

                int targetIdx = targetSerialNo - 1;
                if (targetIdx < 0) targetIdx = 0;
                if (targetIdx > list.Count) targetIdx = list.Count;

                list.Insert(targetIdx, item);

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].SerialNo = i + 1;
                }

                SaveAll(list);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a credential by its unique Id and renumbers remaining items.
        /// </summary>
        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            List<Credential> list = GetAll();
            int countBefore = list.Count;
            list.RemoveAll(delegate(Credential c) { return c.Id == id; });
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

        /// <summary>
        /// Saves all credentials to the database file.
        /// Re-encrypts with AES-256 via active session if master password is set.
        /// </summary>
        public void SaveAll(List<Credential> list)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                if (list != null)
                {
                    foreach (Credential cred in list)
                    {
                        sb.AppendLine(SerializeLine(cred));
                    }
                }

                if (VaultSecurity.IsUnlocked)
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

        /// <summary>
        /// Updates the master password and re-encrypts the entire database vault.
        /// </summary>
        public bool ChangeMasterPassword(string currentPassword, string newPassword, out string error)
        {
            error = null;
            if (!VaultSecurity.VerifyMasterPassword(filePath, currentPassword))
            {
                error = "Current master password is incorrect.";
                return false;
            }

            try
            {
                List<Credential> currentList = GetAll();
                StringBuilder sb = new StringBuilder();
                foreach (Credential c in currentList)
                {
                    sb.AppendLine(SerializeLine(c));
                }

                if (VaultSecurity.ChangeMasterPassword(filePath, currentPassword, newPassword, sb.ToString()))
                {
                    return true;
                }
                else
                {
                    error = "Failed to re-encrypt vault with new password.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string SerializeLine(Credential c)
        {
            string id = Escape(c.Id);
            string service = Escape(c.Service);
            string username = Escape(c.Username);
            string password = Escape(c.Password);
            string updated = c.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
            string serial = c.SerialNo.ToString();

            return string.Format("{0}{1}{2}{1}{3}{1}{4}{1}{5}{1}{6}", id, Delimiter, service, username, password, updated, serial);
        }

        public static Credential DeserializeLine(string line, int defaultSerialNo = 1)
        {
            string[] parts = line.Split(new string[] { Delimiter }, StringSplitOptions.None);
            if (parts.Length >= 5)
            {
                Credential c = new Credential();
                c.Id = Unescape(parts[0]);
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

                int sNo;
                if (parts.Length >= 6 && int.TryParse(parts[5], out sNo) && sNo > 0)
                {
                    c.SerialNo = sNo;
                }
                else
                {
                    c.SerialNo = defaultSerialNo;
                }

                return c;
            }
            return null;
        }

        public static string Escape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(Delimiter, PipeEscape).Replace("\r", "").Replace("\n", " ");
        }

        public static string Unescape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(PipeEscape, Delimiter);
        }
    }
}
