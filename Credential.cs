using System;

namespace PasswordGui
{
    /// <summary>
    /// Represents a stored user credential.
    /// </summary>
    public class Credential
    {
        public string Id { get; set; }
        public int SerialNo { get; set; }
        public string Service { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime LastUpdated { get; set; }

        public Credential()
        {
            Id = Guid.NewGuid().ToString("N");
            SerialNo = 1;
            Service = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            LastUpdated = DateTime.Now;
        }

        public Credential(string service, string username, string password, int serialNo = 1)
        {
            Id = Guid.NewGuid().ToString("N");
            SerialNo = serialNo;
            Service = service ?? string.Empty;
            Username = username ?? string.Empty;
            Password = password ?? string.Empty;
            LastUpdated = DateTime.Now;
        }
    }
}
