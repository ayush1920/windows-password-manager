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

        public CredentialRepository(string customPath = null)
        {
            if (string.IsNullOrEmpty(customPath))
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // If running from bin\, use parent data\ folder
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
                    // Default to parent data if in bin, else local data
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

                filePath = Path.Combine(targetDir, "credentials.txt");
            }
            else
            {
                filePath = customPath;
            }

            EnsureFileInitialized();
        }

        public string GetFilePath()
        {
            return filePath;
        }

        private void EnsureFileInitialized()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# ========================================================");
                    sb.AppendLine("# KeyCraft Plain-Text Credential Store");
                    sb.AppendLine("# Format: Id|Service|Username|Password|LastUpdated");
                    sb.AppendLine("# (You can view and edit this file in Notepad for testing)");
                    sb.AppendLine("# ========================================================");

                    // Add a default sample credential for instant testing
                    Credential sample = new Credential("GitHub", "aykumar", "SampleSecretPass123!");
                    sb.AppendLine(SerializeLine(sample));

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error initializing credential file: " + ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all credentials stored in the plain text file.
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
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue; // Comment line

                    Credential cred = DeserializeLine(trimmed);
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

        /// <summary>
        /// Adds a new credential and saves to the text file.
        /// </summary>
        public void Add(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException("cred");
            List<Credential> list = GetAll();
            cred.LastUpdated = DateTime.Now;
            list.Add(cred);
            SaveAll(list);
        }

        /// <summary>
        /// Updates an existing credential matched by Id.
        /// </summary>
        public bool Update(Credential updatedCred)
        {
            if (updatedCred == null) throw new ArgumentNullException("updatedCred");
            List<Credential> list = GetAll();
            int index = list.FindIndex(delegate (Credential c) { return c.Id == updatedCred.Id; });
            if (index >= 0)
            {
                updatedCred.LastUpdated = DateTime.Now;
                list[index] = updatedCred;
                SaveAll(list);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a credential by its unique Id.
        /// </summary>
        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            List<Credential> list = GetAll();
            int countBefore = list.Count;
            list.RemoveAll(delegate (Credential c) { return c.Id == id; });
            if (list.Count < countBefore)
            {
                SaveAll(list);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Saves all credentials to the plain text file.
        /// </summary>
        public void SaveAll(List<Credential> list)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# ========================================================");
                sb.AppendLine("# KeyCraft Plain-Text Credential Store");
                sb.AppendLine("# Format: Id|Service|Username|Password|LastUpdated");
                sb.AppendLine("# (You can view and edit this file in Notepad for testing)");
                sb.AppendLine("# ========================================================");

                if (list != null)
                {
                    foreach (Credential cred in list)
                    {
                        sb.AppendLine(SerializeLine(cred));
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to write credentials file: " + ex.Message, ex);
            }
        }

        private string SerializeLine(Credential c)
        {
            string id = Escape(c.Id);
            string service = Escape(c.Service);
            string username = Escape(c.Username);
            string password = Escape(c.Password);
            string updated = c.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");

            return string.Format("{0}{1}{2}{1}{3}{1}{4}{1}{5}", id, Delimiter, service, username, password, updated);
        }

        private Credential DeserializeLine(string line)
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
                return c;
            }
            return null;
        }

        private string Escape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(Delimiter, PipeEscape).Replace("\r", "").Replace("\n", " ");
        }

        private string Unescape(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Replace(PipeEscape, Delimiter);
        }
    }
}
