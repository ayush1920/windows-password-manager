using System;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Legacy compatibility wrapper for MasterPasswordForm in Unlock mode.
    /// Delegates directly to the unified MasterPasswordForm to uphold DRY (Do Not Repeat Yourself)
    /// and eliminate parallel UI technical debt.
    /// </summary>
    public class UnlockForm : MasterPasswordForm
    {
        public bool IsNewVaultRequested { get; set; }

        public UnlockForm(CredentialService credService, string initialPath = null)
            : base(MasterPasswordMode.Unlock, initialPath ?? (credService != null ? credService.StorageFilePath : null), inAppContext: false)
        {
        }
    }
}
