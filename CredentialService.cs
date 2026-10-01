using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGui
{
    public enum FilterMode
    {
        All,
        Service,
        Username,
        Weak
    }

    /// <summary>
    /// Business and program logic for credentials management, validation, search, and password generation.
    /// </summary>
    public class CredentialService
    {
        private readonly CredentialRepository repository;

        public CredentialService(CredentialRepository repo)
        {
            if (repo == null) throw new ArgumentNullException("repo");
            this.repository = repo;
        }

        public bool HasActiveVault
        {
            get { return repository != null && repository.HasActiveVault; }
        }

        public string StorageFilePath
        {
            get { return repository != null ? repository.GetFilePath() : null; }
        }

        public string GetVaultFilePath()
        {
            return StorageFilePath;
        }

        public void SwitchDatabase(string newPath)
        {
            if (repository != null)
            {
                repository.SwitchDatabase(newPath);
            }
        }

        public List<Credential> LoadAll()
        {
            return repository != null ? repository.GetAll() : new List<Credential>();
        }

        public bool Validate(string service, string username, string password, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(service))
            {
                errorMessage = "Service or Website name cannot be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Password cannot be empty.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public void AddCredential(string service, string username, string password, string notes = "")
        {
            string err;
            if (!Validate(service, username, password, out err))
            {
                throw new ArgumentException(err);
            }

            Credential cred = new Credential(service.Trim(), username != null ? username.Trim() : "", password, notes != null ? notes.Trim() : "");
            repository.Add(cred);
        }

        public void UpdateCredential(string id, string service, string username, string password, string notes = "")
        {
            string err;
            if (!Validate(service, username, password, out err))
            {
                throw new ArgumentException(err);
            }

            Credential cred = new Credential();
            cred.Id = id;
            cred.Service = service.Trim();
            cred.Username = username != null ? username.Trim() : "";
            cred.Password = password;
            cred.Notes = notes != null ? notes.Trim() : "";
            cred.LastUpdated = DateTime.Now;

            bool updated = repository.Update(cred);
            if (!updated)
            {
                throw new InvalidOperationException("Credential could not be found to update.");
            }
        }

        public bool DeleteCredential(string id)
        {
            return repository.Delete(id);
        }

        public bool HasMasterPassword()
        {
            return repository.HasMasterPassword();
        }

        public bool UnlockVault(string masterPassword)
        {
            return repository.VerifyMasterPassword(masterPassword);
        }

        public void LockVault()
        {
            VaultSecurity.LockSession();
            if (repository != null)
            {
                repository.SwitchDatabase(null);
            }
        }

        public bool ReorderCredential(string id, int targetSerialNo)
        {
            if (string.IsNullOrEmpty(id) || repository == null) return false;
            List<Credential> list = repository.GetAll();
            int currentIndex = list.FindIndex(delegate (Credential c) { return c.Id == id; });
            if (currentIndex < 0) return false;

            Credential item = list[currentIndex];
            list.RemoveAt(currentIndex);

            int targetIndex = targetSerialNo - 1;
            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex > list.Count) targetIndex = list.Count;

            list.Insert(targetIndex, item);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].SerialNo = i + 1;
            }

            repository.SaveAll(list);
            return true;
        }

        public bool ChangeMasterPassword(string currentPassword, string newPassword, string confirmPassword, out string error)
        {
            if (!HasActiveVault)
            {
                error = "No vault is currently loaded. Please open or create a vault first.";
                return false;
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                error = "New master password must be at least 6 characters long.";
                return false;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                error = "New master password and confirmation do not match.";
                return false;
            }

            return repository.ChangeMasterPassword(currentPassword, newPassword, out error);
        }

        public string GetSetting(string key, string defVal = "")
        {
            return repository.GetSetting(key, defVal);
        }

        public void SaveSetting(string key, string val)
        {
            repository.SetSetting(key, val);
        }

        /// <summary>
        /// Filters a list of credentials by a search keyword and filter criteria.
        /// </summary>
        public List<Credential> Filter(List<Credential> source, string query, FilterMode mode = FilterMode.All)
        {
            if (source == null) return new List<Credential>();

            string q = !string.IsNullOrWhiteSpace(query) ? query.Trim().ToLowerInvariant() : null;
            List<Credential> results = new List<Credential>();

            foreach (Credential c in source)
            {
                // 1. Check filter criteria
                if (mode == FilterMode.Weak)
                {
                    string strengthLabel;
                    int score = EvaluateStrength(c.Password, out strengthLabel);
                    if (score > 1) continue; // Skip non-weak
                }

                // 2. Check query keyword
                if (string.IsNullOrEmpty(q))
                {
                    results.Add(c);
                    continue;
                }

                bool matchService = !string.IsNullOrEmpty(c.Service) && c.Service.ToLowerInvariant().Contains(q);
                bool matchUser = !string.IsNullOrEmpty(c.Username) && c.Username.ToLowerInvariant().Contains(q);

                if (mode == FilterMode.Service)
                {
                    if (matchService) results.Add(c);
                }
                else if (mode == FilterMode.Username)
                {
                    if (matchUser) results.Add(c);
                }
                else
                {
                    if (matchService || matchUser) results.Add(c);
                }
            }

            return results;
        }

        /// <summary>
        /// Filters a list of credentials by a search query and a string field name ("Service", "Username", "Weak", "Sl No").
        /// </summary>
        public List<Credential> Filter(List<Credential> source, string query, string field)
        {
            if (string.Equals(field, "Service", StringComparison.OrdinalIgnoreCase))
                return Filter(source, query, FilterMode.Service);
            if (string.Equals(field, "Username", StringComparison.OrdinalIgnoreCase))
                return Filter(source, query, FilterMode.Username);
            if (string.Equals(field, "Weak", StringComparison.OrdinalIgnoreCase))
                return Filter(source, query, FilterMode.Weak);
            if (string.Equals(field, "Sl No", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(field, "SlNo", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(field, "SerialNo", StringComparison.OrdinalIgnoreCase))
            {
                if (source == null) return new List<Credential>();
                if (string.IsNullOrWhiteSpace(query)) return new List<Credential>(source);
                int targetSl;
                if (!int.TryParse(query.Trim(), out targetSl)) return new List<Credential>();
                return source.FindAll(delegate(Credential c) { return c.SerialNo == targetSl; });
            }
            return Filter(source, query, FilterMode.All);
        }

        /// <summary>
        /// Generates a cryptographically secure random password.
        /// </summary>
        public string GeneratePassword(int length = 16)
        {
            const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowers = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*()-_=+";

            string all = uppers + lowers + digits + symbols;
            StringBuilder sb = new StringBuilder();

            sb.Append(GetRandomChar(uppers));
            sb.Append(GetRandomChar(lowers));
            sb.Append(GetRandomChar(digits));
            sb.Append(GetRandomChar(symbols));

            for (int i = 4; i < length; i++)
            {
                sb.Append(GetRandomChar(all));
            }

            char[] chars = sb.ToString().ToCharArray();
            Shuffle(chars);
            return new string(chars);
        }

        /// <summary>
        /// Evaluates password strength: 1 (Weak), 2 (Medium/Fair), 3 (Good), 4 (Strong).
        /// </summary>
        public int EvaluateStrength(string pwd, out string label)
        {
            if (string.IsNullOrEmpty(pwd))
            {
                label = "None";
                return 0;
            }

            int score = 0;
            if (pwd.Length >= 8) score++;
            if (pwd.Length >= 12) score++;
            if (pwd.Length >= 16) score++;

            bool hasUpper = false, hasLower = false, hasDigit = false, hasSymbol = false;
            foreach (char c in pwd)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSymbol = true;
            }

            int varieties = 0;
            if (hasUpper) varieties++;
            if (hasLower) varieties++;
            if (hasDigit) varieties++;
            if (hasSymbol) varieties++;

            if (varieties >= 3) score++;
            if (varieties == 4) score++;

            if (pwd.Length < 8 || varieties <= 1)
            {
                label = "Weak";
                return 1;
            }
            if (score <= 2)
            {
                label = "Medium";
                return 2;
            }
            if (score <= 4)
            {
                label = "Good";
                return 3;
            }
            label = "Strong";
            return 4;
        }

        private char GetRandomChar(string charset)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                uint val = BitConverter.ToUInt32(bytes, 0);
                return charset[(int)(val % (uint)charset.Length)];
            }
        }

        private void Shuffle(char[] array)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] box = new byte[4];
                for (int i = array.Length - 1; i > 0; i--)
                {
                    rng.GetBytes(box);
                    int j = (int)(BitConverter.ToUInt32(box, 0) % (uint)(i + 1));
                    char temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }
        }
    }
}
