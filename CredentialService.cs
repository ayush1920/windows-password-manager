using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGui
{
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

        public string StorageFilePath
        {
            get { return repository.GetFilePath(); }
        }

        public List<Credential> LoadAll()
        {
            return repository.GetAll();
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

        public void AddCredential(string service, string username, string password)
        {
            string err;
            if (!Validate(service, username, password, out err))
            {
                throw new ArgumentException(err);
            }

            Credential cred = new Credential(service.Trim(), username != null ? username.Trim() : "", password);
            repository.Add(cred);
        }

        public void UpdateCredential(string id, string service, string username, string password)
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

        /// <summary>
        /// Filters a list of credentials by a search keyword (matches Service or Username).
        /// </summary>
        public List<Credential> Filter(List<Credential> source, string query)
        {
            if (source == null) return new List<Credential>();
            if (string.IsNullOrWhiteSpace(query)) return new List<Credential>(source);

            string q = query.Trim().ToLowerInvariant();
            List<Credential> results = new List<Credential>();

            foreach (Credential c in source)
            {
                bool matchService = !string.IsNullOrEmpty(c.Service) && c.Service.ToLowerInvariant().Contains(q);
                bool matchUser = !string.IsNullOrEmpty(c.Username) && c.Username.ToLowerInvariant().Contains(q);
                if (matchService || matchUser)
                {
                    results.Add(c);
                }
            }

            return results;
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
        /// Evaluates password strength: 1 (Weak), 2 (Fair), 3 (Good), 4 (Strong).
        /// </summary>
        public int EvaluateStrength(string pwd, out string label)
        {
            if (string.IsNullOrEmpty(pwd))
            {
                label = "Empty";
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
                label = "Fair";
                return 2;
            }
            if (score <= 4)
            {
                label = "Good";
                return 3;
            }
            label = "Very Strong";
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
