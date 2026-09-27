using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PasswordGui
{
    public enum MasterPasswordMode
    {
        Create,
        Unlock
    }

    /// <summary>
    /// Master Password Dialog shown at application startup and vault switching.
    /// Provides KeePass-style multi-vault management:
    /// - Displays target vault file with Browse and New Vault options.
    /// - Prevents user entrapment if password is forgotten.
    /// - In Create mode: sets up master password and encrypts vault with AES-256.
    /// - In Unlock mode: verifies master password via HMAC-SHA256 and unlocks vault.
    /// </summary>
    public class MasterPasswordForm : Form
    {
        private MasterPasswordMode mode;
        private string vaultFilePath;
        public string SelectedVaultPath { get; private set; }
        public MasterPasswordMode Mode { get { return mode; } }

        // Visual Colors (Obsidian Theme)
        private static readonly Color ColorBgApp = Color.FromArgb(15, 17, 23);
        private static readonly Color ColorBgCard = Color.FromArgb(20, 24, 33);
        private static readonly Color ColorBorder = Color.FromArgb(37, 44, 65);
        private static readonly Color ColorPrimary = Color.FromArgb(99, 102, 241);
        private static readonly Color ColorPrimaryHover = Color.FromArgb(129, 140, 248);
        private static readonly Color ColorTextPrimary = Color.FromArgb(243, 244, 246);
        private static readonly Color ColorTextMuted = Color.FromArgb(156, 163, 175);
        private static readonly Color ColorDanger = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorSuccess = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorSecondary = Color.FromArgb(31, 35, 48);
        private static readonly Color ColorSecondaryHover = Color.FromArgb(41, 47, 66);

        // UI Controls
        private Panel contentPanel;
        private Panel titleBar;
        private Label lblTitle;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private Label lblError;
        private Panel strengthBar;
        private Label lblStrength;
        private Button btnTogglePassword;
        private bool isPasswordRevealed = false;

        public MasterPasswordForm(MasterPasswordMode mode, string vaultFilePath)
        {
            this.mode = mode;
            this.vaultFilePath = string.IsNullOrEmpty(vaultFilePath) ? AppSettings.GetDefaultVaultPath() : Path.GetFullPath(vaultFilePath);
            this.SelectedVaultPath = this.vaultFilePath;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBgApp;
            this.ForeColor = ColorTextPrimary;
            this.Font = new Font("Segoe UI", 9.5f);
            this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 530) : new Size(480, 470);
            this.KeyPreview = true;

            // Outer Border Painting
            this.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen borderPen = new Pen(ColorBorder, 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // Custom Title / Drag Bar
            titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 40;
            titleBar.BackColor = Color.FromArgb(12, 14, 19);
            titleBar.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            };

            // Shield / Lock Icon
            Panel iconPanel = new Panel();
            iconPanel.Location = new Point(14, 12);
            iconPanel.Size = new Size(16, 16);
            iconPanel.BackColor = Color.Transparent;
            iconPanel.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon(mode == MasterPasswordMode.Create ? "shield" : "lock");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), ColorPrimaryHover);
                }
            };
            titleBar.Controls.Add(iconPanel);

            lblTitle = new Label();
            lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Vault Setup" : "KeyCraft — Vault Locked";
            lblTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblTitle.ForeColor = ColorTextMuted;
            lblTitle.Location = new Point(38, 11);
            lblTitle.AutoSize = true;
            lblTitle.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            };
            titleBar.Controls.Add(lblTitle);

            // Close button
            Button btnClose = new Button();
            btnClose.Text = "✕";
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 44;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = ColorTextMuted;
            btnClose.BackColor = Color.Transparent;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
            titleBar.Controls.Add(btnClose);

            this.Controls.Add(titleBar);

            // Content Panel
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(28, 16, 28, 16);
            this.Controls.Add(contentPanel);
            contentPanel.BringToFront();

            BuildContent();
        }

        private void RebuildUI()
        {
            this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 530) : new Size(480, 470);
            lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Vault Setup" : "KeyCraft — Vault Locked";
            titleBar.Invalidate();
            contentPanel.Controls.Clear();
            BuildContent();
            this.Invalidate();
        }

        private void BuildContent()
        {
            int curY = 10;

            // 1. Vault File Card (KeePass Style)
            Panel cardVault = new Panel();
            cardVault.Location = new Point(28, curY);
            cardVault.Size = new Size(contentPanel.Width - 56, 52);
            cardVault.BackColor = ColorBgCard;
            cardVault.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, cardVault.Width - 1, cardVault.Height - 1);
                }
            };

            Label lblVaultHeader = new Label();
            lblVaultHeader.Text = "DATABASE:";
            lblVaultHeader.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblVaultHeader.ForeColor = ColorPrimary;
            lblVaultHeader.Location = new Point(10, 6);
            lblVaultHeader.AutoSize = true;
            cardVault.Controls.Add(lblVaultHeader);

            string fileName = Path.GetFileName(vaultFilePath);
            Label lblFileName = new Label();
            lblFileName.Text = string.IsNullOrEmpty(fileName) ? "New Vault" : fileName;
            lblFileName.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblFileName.ForeColor = ColorTextPrimary;
            lblFileName.Location = new Point(74, 5);
            lblFileName.Size = new Size(cardVault.Width - 210, 18);
            cardVault.Controls.Add(lblFileName);

            string dirPath = Path.GetDirectoryName(vaultFilePath);
            Label lblDir = new Label();
            lblDir.Text = dirPath ?? string.Empty;
            lblDir.Font = new Font("Segoe UI", 7.5f);
            lblDir.ForeColor = ColorTextMuted;
            lblDir.Location = new Point(10, 26);
            lblDir.Size = new Size(cardVault.Width - 146, 18);
            cardVault.Controls.Add(lblDir);

            ToolTip tt = new ToolTip();
            tt.SetToolTip(lblDir, vaultFilePath);
            tt.SetToolTip(lblFileName, vaultFilePath);

            // Browse button
            Button btnBrowse = new Button();
            btnBrowse.Text = "Browse";
            btnBrowse.Location = new Point(cardVault.Width - 132, 10);
            btnBrowse.Size = new Size(60, 30);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.BackColor = ColorSecondary;
            btnBrowse.ForeColor = ColorTextPrimary;
            btnBrowse.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnBrowse.Cursor = Cursors.Hand;
            btnBrowse.Click += delegate { HandleBrowseVault(); };
            cardVault.Controls.Add(btnBrowse);

            // New Vault button
            Button btnNew = new Button();
            btnNew.Text = "New...";
            btnNew.Location = new Point(cardVault.Width - 66, 10);
            btnNew.Size = new Size(56, 30);
            btnNew.FlatStyle = FlatStyle.Flat;
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.BackColor = ColorSecondary;
            btnNew.ForeColor = ColorPrimary;
            btnNew.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnNew.Cursor = Cursors.Hand;
            btnNew.Click += delegate { HandleNewVault(); };
            cardVault.Controls.Add(btnNew);

            contentPanel.Controls.Add(cardVault);
            curY += 60;

            // 2. Heading
            Label lblHeader = new Label();
            lblHeader.Text = (mode == MasterPasswordMode.Create) ? "Create Master Password" : "Enter Master Password";
            lblHeader.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblHeader.ForeColor = ColorTextPrimary;
            lblHeader.Location = new Point(28, curY);
            lblHeader.AutoSize = true;
            contentPanel.Controls.Add(lblHeader);
            curY += 26;

            Label lblSub = new Label();
            lblSub.Text = (mode == MasterPasswordMode.Create)
                ? "Protect this database with AES-256 encryption. Only this master password can unlock it."
                : "Enter your master password to unlock and access your stored credentials.";
            lblSub.Font = new Font("Segoe UI", 8.5f);
            lblSub.ForeColor = ColorTextMuted;
            lblSub.Location = new Point(28, curY);
            lblSub.Size = new Size(contentPanel.Width - 56, 32);
            contentPanel.Controls.Add(lblSub);
            curY += 36;

            // 3. Password Input Label
            Label lblPassLabel = new Label();
            lblPassLabel.Text = (mode == MasterPasswordMode.Create) ? "MASTER PASSWORD *" : "MASTER PASSWORD";
            lblPassLabel.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblPassLabel.ForeColor = ColorTextMuted;
            lblPassLabel.Location = new Point(28, curY);
            lblPassLabel.AutoSize = true;
            contentPanel.Controls.Add(lblPassLabel);
            curY += 20;

            // Password Container (with toggle eye button)
            Panel passContainer = new Panel();
            passContainer.Location = new Point(28, curY);
            passContainer.Size = new Size(contentPanel.Width - 56, 38);
            passContainer.BackColor = Color.FromArgb(17, 24, 39);
            passContainer.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, passContainer.Width - 1, passContainer.Height - 1);
                }
            };

            txtPassword = new TextBox();
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.BackColor = Color.FromArgb(17, 24, 39);
            txtPassword.ForeColor = ColorTextPrimary;
            txtPassword.Font = new Font("Segoe UI", 10.5f);
            txtPassword.Location = new Point(10, 9);
            txtPassword.Width = passContainer.Width - 48;
            txtPassword.PasswordChar = '●';
            txtPassword.TextChanged += delegate
            {
                lblError.Visible = false;
                if (mode == MasterPasswordMode.Create) UpdateStrength();
            };
            passContainer.Controls.Add(txtPassword);

            btnTogglePassword = new Button();
            btnTogglePassword.Dock = DockStyle.Right;
            btnTogglePassword.Width = 36;
            btnTogglePassword.FlatStyle = FlatStyle.Flat;
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.BackColor = Color.Transparent;
            btnTogglePassword.Cursor = Cursors.Hand;
            btnTogglePassword.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon(isPasswordRevealed ? "eye_off" : "eye");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(10, 11, 16, 16), ColorTextMuted);
                }
            };
            btnTogglePassword.Click += delegate
            {
                isPasswordRevealed = !isPasswordRevealed;
                txtPassword.PasswordChar = isPasswordRevealed ? '\0' : '●';
                btnTogglePassword.Invalidate();
            };
            passContainer.Controls.Add(btnTogglePassword);

            contentPanel.Controls.Add(passContainer);
            curY += 44;

            if (mode == MasterPasswordMode.Create)
            {
                // Password Strength Indicator
                Panel strengthWrapper = new Panel();
                strengthWrapper.Location = new Point(28, curY);
                strengthWrapper.Size = new Size(contentPanel.Width - 56, 18);
                strengthWrapper.BackColor = Color.Transparent;

                strengthBar = new Panel();
                strengthBar.Location = new Point(0, 7);
                strengthBar.Size = new Size(180, 4);
                strengthBar.BackColor = Color.FromArgb(37, 44, 65);
                strengthWrapper.Controls.Add(strengthBar);

                lblStrength = new Label();
                lblStrength.Location = new Point(190, 0);
                lblStrength.Size = new Size(180, 18);
                lblStrength.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                lblStrength.ForeColor = ColorTextMuted;
                lblStrength.Text = "Strength: None";
                strengthWrapper.Controls.Add(lblStrength);

                contentPanel.Controls.Add(strengthWrapper);
                curY += 24;

                // Confirm Password Label
                Label lblConfirmLabel = new Label();
                lblConfirmLabel.Text = "CONFIRM MASTER PASSWORD *";
                lblConfirmLabel.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                lblConfirmLabel.ForeColor = ColorTextMuted;
                lblConfirmLabel.Location = new Point(28, curY);
                lblConfirmLabel.AutoSize = true;
                contentPanel.Controls.Add(lblConfirmLabel);
                curY += 20;

                // Confirm Password Container
                Panel confirmContainer = new Panel();
                confirmContainer.Location = new Point(28, curY);
                confirmContainer.Size = new Size(contentPanel.Width - 56, 38);
                confirmContainer.BackColor = Color.FromArgb(17, 24, 39);
                confirmContainer.Paint += delegate(object s, PaintEventArgs pe)
                {
                    using (Pen p = new Pen(ColorBorder, 1f))
                    {
                        pe.Graphics.DrawRectangle(p, 0, 0, confirmContainer.Width - 1, confirmContainer.Height - 1);
                    }
                };

                txtConfirm = new TextBox();
                txtConfirm.BorderStyle = BorderStyle.None;
                txtConfirm.BackColor = Color.FromArgb(17, 24, 39);
                txtConfirm.ForeColor = ColorTextPrimary;
                txtConfirm.Font = new Font("Segoe UI", 10.5f);
                txtConfirm.Location = new Point(10, 9);
                txtConfirm.Width = confirmContainer.Width - 20;
                txtConfirm.PasswordChar = '●';
                txtConfirm.TextChanged += delegate { lblError.Visible = false; };
                confirmContainer.Controls.Add(txtConfirm);

                contentPanel.Controls.Add(confirmContainer);
                curY += 46;
            }

            // Error Label
            lblError = new Label();
            lblError.Location = new Point(28, curY);
            lblError.Size = new Size(contentPanel.Width - 56, 26);
            lblError.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lblError.ForeColor = ColorDanger;
            lblError.Visible = false;
            contentPanel.Controls.Add(lblError);
            curY += 28;

            // Submit Button
            Button btnSubmit = new Button();
            btnSubmit.Text = (mode == MasterPasswordMode.Create) ? "Create Encrypted Vault" : "Unlock Vault";
            btnSubmit.Location = new Point(28, curY);
            btnSubmit.Size = new Size(contentPanel.Width - 56, 38);
            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.BackColor = ColorPrimary;
            btnSubmit.ForeColor = Color.White;
            btnSubmit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSubmit.Cursor = Cursors.Hand;
            btnSubmit.Click += delegate { HandleSubmit(); };
            contentPanel.Controls.Add(btnSubmit);
            curY += 44;

            // KeePass Fallback Action Buttons in Unlock Mode
            if (mode == MasterPasswordMode.Unlock)
            {
                Panel pnlAlt = new Panel();
                pnlAlt.Location = new Point(28, curY);
                pnlAlt.Size = new Size(contentPanel.Width - 56, 32);
                pnlAlt.BackColor = Color.Transparent;

                Button btnAltOpen = new Button();
                btnAltOpen.Text = "📂 Open Other Vault";
                btnAltOpen.Location = new Point(0, 0);
                btnAltOpen.Size = new Size((pnlAlt.Width - 8) / 2, 30);
                btnAltOpen.FlatStyle = FlatStyle.Flat;
                btnAltOpen.FlatAppearance.BorderSize = 0;
                btnAltOpen.BackColor = ColorSecondary;
                btnAltOpen.ForeColor = ColorTextMuted;
                btnAltOpen.Font = new Font("Segoe UI", 8.5f);
                btnAltOpen.Cursor = Cursors.Hand;
                btnAltOpen.Click += delegate { HandleBrowseVault(); };
                pnlAlt.Controls.Add(btnAltOpen);

                Button btnAltNew = new Button();
                btnAltNew.Text = "➕ Create New Vault";
                btnAltNew.Location = new Point((pnlAlt.Width - 8) / 2 + 8, 0);
                btnAltNew.Size = new Size((pnlAlt.Width - 8) / 2, 30);
                btnAltNew.FlatStyle = FlatStyle.Flat;
                btnAltNew.FlatAppearance.BorderSize = 0;
                btnAltNew.BackColor = ColorSecondary;
                btnAltNew.ForeColor = ColorTextMuted;
                btnAltNew.Font = new Font("Segoe UI", 8.5f);
                btnAltNew.Cursor = Cursors.Hand;
                btnAltNew.Click += delegate { HandleNewVault(); };
                pnlAlt.Controls.Add(btnAltNew);

                contentPanel.Controls.Add(pnlAlt);
                curY += 36;
            }

            // Cancel / Exit Button
            Button btnCancel = new Button();
            btnCancel.Text = "Exit";
            btnCancel.Location = new Point(28, curY);
            btnCancel.Size = new Size(contentPanel.Width - 56, 28);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.BackColor = Color.Transparent;
            btnCancel.ForeColor = ColorTextMuted;
            btnCancel.Font = new Font("Segoe UI", 8.5f);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
            contentPanel.Controls.Add(btnCancel);

            // Focus
            txtPassword.Focus();
            txtPassword.SelectAll();
        }

        private void HandleBrowseVault()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Open KeyCraft Vault Database";
                ofd.Filter = "KeyCraft Vault (*.kcrypt;*.kdb;*.txt)|*.kcrypt;*.kdb;*.txt|All Files (*.*)|*.*";
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    SwitchVault(ofd.FileName);
                }
            }
        }

        private void HandleNewVault()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Create New KeyCraft Vault Database";
                sfd.Filter = "KeyCraft Encrypted Vault (*.kcrypt)|*.kcrypt|All Files (*.*)|*.*";
                sfd.DefaultExt = "kcrypt";
                sfd.AddExtension = true;
                sfd.FileName = "vault.kcrypt";
                string defDir = Path.GetDirectoryName(vaultFilePath);
                if (!string.IsNullOrEmpty(defDir) && Directory.Exists(defDir))
                {
                    sfd.InitialDirectory = defDir;
                }

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    SwitchToNewVault(sfd.FileName);
                }
            }
        }

        public void SwitchVault(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            this.vaultFilePath = Path.GetFullPath(path);
            this.SelectedVaultPath = this.vaultFilePath;
            bool isEncrypted = VaultSecurity.IsVaultEncrypted(this.vaultFilePath);
            this.mode = isEncrypted ? MasterPasswordMode.Unlock : MasterPasswordMode.Create;
            RebuildUI();
        }

        public void SwitchToNewVault(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            this.vaultFilePath = Path.GetFullPath(path);
            this.SelectedVaultPath = this.vaultFilePath;
            this.mode = MasterPasswordMode.Create;
            RebuildUI();
        }

        private void UpdateStrength()
        {
            if (strengthBar == null || lblStrength == null) return;
            string pwd = txtPassword.Text;

            if (string.IsNullOrEmpty(pwd))
            {
                strengthBar.BackColor = Color.FromArgb(37, 44, 65);
                strengthBar.Width = 40;
                lblStrength.Text = "Strength: None";
                lblStrength.ForeColor = ColorTextMuted;
                return;
            }

            int score = 0;
            if (pwd.Length >= 6) score++;
            if (pwd.Length >= 10) score++;
            if (Regex.IsMatch(pwd, @"[A-Z]")) score++;
            if (Regex.IsMatch(pwd, @"[0-9]")) score++;
            if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            if (score <= 1)
            {
                strengthBar.BackColor = ColorDanger;
                strengthBar.Width = 50;
                lblStrength.Text = "Strength: Weak";
                lblStrength.ForeColor = ColorDanger;
            }
            else if (score <= 3)
            {
                strengthBar.BackColor = Color.FromArgb(245, 158, 11);
                strengthBar.Width = 110;
                lblStrength.Text = "Strength: Medium";
                lblStrength.ForeColor = Color.FromArgb(245, 158, 11);
            }
            else
            {
                strengthBar.BackColor = ColorSuccess;
                strengthBar.Width = 180;
                lblStrength.Text = "Strength: Strong";
                lblStrength.ForeColor = ColorSuccess;
            }
        }

        private void HandleSubmit()
        {
            string password = txtPassword.Text;

            if (mode == MasterPasswordMode.Create)
            {
                if (string.IsNullOrEmpty(password) || password.Length < 6)
                {
                    ShowError("Master password must be at least 6 characters.");
                    txtPassword.Focus();
                    return;
                }

                if (txtConfirm != null && password != txtConfirm.Text)
                {
                    ShowError("Passwords do not match. Please verify.");
                    txtConfirm.Focus();
                    return;
                }

                try
                {
                    // If existing file is present and has plain text lines, migrate them!
                    string existingPlainText = string.Empty;
                    if (File.Exists(vaultFilePath))
                    {
                        existingPlainText = File.ReadAllText(vaultFilePath, System.Text.Encoding.UTF8);
                    }

                    VaultSecurity.InitializeAndEncryptVault(vaultFilePath, password, existingPlainText);
                    SelectedVaultPath = vaultFilePath;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    ShowError("Failed to initialize encrypted vault: " + ex.Message);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Please enter your master password.");
                    txtPassword.Focus();
                    return;
                }

                string decrypted;
                if (VaultSecurity.UnlockVault(vaultFilePath, password, out decrypted))
                {
                    SelectedVaultPath = vaultFilePath;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError("Incorrect master password. Forgot password? Click 'Create New Vault' below.");
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                }
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = "⚠ " + msg;
            lblError.Visible = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                HandleSubmit();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            if (keyData == (Keys.Control | Keys.O))
            {
                HandleBrowseVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.N))
            {
                HandleNewVault();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Native Dragging Support
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
    }
}
