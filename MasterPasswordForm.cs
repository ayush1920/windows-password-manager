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
        private bool inAppContext;
        public string SelectedVaultPath { get; private set; }
        public string DecryptedSessionText { get; private set; }
        public string ActiveMasterPassword { get; private set; }
        public MasterPasswordMode Mode { get { return mode; } }
        public bool InAppContext { get { return inAppContext; } }

        // Visual Colors (Windows 11 Fluent Theme matching MainForm)
        private static readonly Color ColorBgApp = WinColors.Window;
        private static readonly Color ColorBgCard = WinColors.Card;
        private static readonly Color ColorBorder = WinColors.Border;
        private static readonly Color ColorPrimary = WinColors.Accent;
        private static readonly Color ColorPrimaryHover = WinColors.AccentHover;
        private static readonly Color ColorTextPrimary = WinColors.TextWhite;
        private static readonly Color ColorTextMuted = WinColors.TextSecondary;
        private static readonly Color ColorDanger = WinColors.WeakText;
        private static readonly Color ColorSuccess = Color.FromArgb(34, 197, 94); // #22C55E vibrant success green
        private static readonly Color ColorSecondary = WinColors.InputBg;
        private static readonly Color ColorSecondaryHover = WinColors.BtnSecondaryHover;

        // UI Controls
        private Panel contentPanel;
        private Panel titleBar;
        private Label lblTitle;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private Label lblError;
        private Panel strengthBar;
        private Label lblStrength;
        private int strengthScore = 0;
        private Button btnTogglePassword;
        private bool isPasswordRevealed = false;

        public MasterPasswordForm(MasterPasswordMode mode, string vaultFilePath, bool inAppContext = false)
        {
            this.mode = mode;
            this.inAppContext = inAppContext;
            this.vaultFilePath = string.IsNullOrEmpty(vaultFilePath) ? AppSettings.GetDefaultVaultPath() : Path.GetFullPath(vaultFilePath);
            this.SelectedVaultPath = this.vaultFilePath;

            InitializeComponent();
            SingleInstanceController.RegisterActiveForm(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SingleInstanceController.UnregisterActiveForm(this);
            base.OnFormClosed(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= Win32Helper.CS_DROPSHADOW;
                cp.Style |= Win32Helper.WS_MINIMIZEBOX;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win32Helper.ApplyWindowShadow(this.Handle);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Win32Helper.ApplyWindowShadow(this.Handle);
            if (txtPassword != null)
            {
                this.ActiveControl = txtPassword;
                txtPassword.Focus();
                txtPassword.SelectAll();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (txtPassword != null)
            {
                this.ActiveControl = txtPassword;
                txtPassword.Focus();
                txtPassword.SelectAll();
            }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBgApp;
            this.ForeColor = ColorTextPrimary;
            this.Font = new Font("Segoe UI", 9.5f);
            if (inAppContext)
            {
                this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 410) : new Size(480, 280);
            }
            else
            {
                this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 410) : new Size(480, 320);
            }
            this.KeyPreview = true;

            // Outer Border Painting
            this.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen borderPen = new Pen(ColorBorder, 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
            this.Resize += delegate { this.Invalidate(); };

            // Custom Title / Drag Bar
            titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 34;
            titleBar.BackColor = WinColors.Chrome;
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
            iconPanel.Location = new Point(12, 9);
            iconPanel.Size = new Size(16, 16);
            iconPanel.BackColor = Color.Transparent;
            iconPanel.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon(mode == MasterPasswordMode.Create ? "shield" : "lock");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), WinColors.Accent);
                }
            };
            titleBar.Controls.Add(iconPanel);

            lblTitle = new Label();
            if (inAppContext)
            {
                lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Set Master Password" : "KeyCraft — Unlock Vault";
            }
            else
            {
                lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Vault Setup" : "KeyCraft — Vault Locked";
            }
            lblTitle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblTitle.ForeColor = WinColors.TextSecondary;
            lblTitle.Location = new Point(34, 8);
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

            // Close button (Fluent TitleBarButton)
            TitleBarButton btnClose = new TitleBarButton(TitleButtonType.Close);
            btnClose.Dock = DockStyle.Right;
            btnClose.Size = new Size(46, 34);
            btnClose.Click += delegate
            {
                // When in app context, cancel returns to main window.
                // At startup, closing the unlock dialog signals opening MainForm with No Vault Loaded.
                this.DialogResult = inAppContext ? DialogResult.Cancel : DialogResult.Ignore;
                this.Close();
            };
            titleBar.Controls.Add(btnClose);

            // 1px subtle divider
            Panel pnlTitleDiv = new Panel();
            pnlTitleDiv.Dock = DockStyle.Bottom;
            pnlTitleDiv.Height = 1;
            pnlTitleDiv.BackColor = WinColors.BorderSubtle;
            titleBar.Controls.Add(pnlTitleDiv);

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
            if (inAppContext)
            {
                this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 410) : new Size(480, 280);
                lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Set Master Password" : "KeyCraft — Unlock Vault";
            }
            else
            {
                this.Size = (mode == MasterPasswordMode.Create) ? new Size(480, 410) : new Size(480, 320);
                lblTitle.Text = mode == MasterPasswordMode.Create ? "KeyCraft — Vault Setup" : "KeyCraft — Vault Locked";
            }
            titleBar.Invalidate();
            contentPanel.Controls.Clear();
            BuildContent();
            this.Invalidate();
        }

        private void BuildContent()
        {
            int curY = 10;

            // 1. Vault File Card (KeePass Style) - Compact, darker, seamless icon with +3px top/bottom padding
            Panel cardVault = new Panel();
            cardVault.Location = new Point(28, curY);
            cardVault.Size = new Size(contentPanel.Width - 56, 42);
            cardVault.BackColor = ColorSecondary;
            cardVault.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, cardVault.Width - 1, cardVault.Height - 1);
                }
            };

            // DB Icon (No border, same background as cardVault, +3px top padding)
            Panel iconDb = new Panel();
            iconDb.Location = new Point(10, 11);
            iconDb.Size = new Size(20, 20);
            iconDb.BackColor = Color.Transparent;
            iconDb.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon("database");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(1, 1, 18, 18), WinColors.Accent);
                }
            };
            cardVault.Controls.Add(iconDb);

            string fileName = Path.GetFileName(vaultFilePath);
            Label lblFileName = new Label();
            lblFileName.Text = string.IsNullOrEmpty(fileName) ? "New Vault" : fileName;
            lblFileName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblFileName.ForeColor = ColorTextPrimary;
            lblFileName.Location = new Point(36, 12);
            lblFileName.AutoSize = true;
            cardVault.Controls.Add(lblFileName);

            ToolTip tt = new ToolTip();
            tt.SetToolTip(iconDb, vaultFilePath);
            tt.SetToolTip(lblFileName, vaultFilePath);

            if (!inAppContext)
            {
                // Browse button (+3px top padding)
                ModernButton btnBrowse = new ModernButton();
                btnBrowse.Text = "Browse";
                btnBrowse.IconName = "folder";
                btnBrowse.IconSize = 12;
                btnBrowse.ShowFocusBorder = false;
                btnBrowse.Location = new Point(cardVault.Width - 156, 7);
                btnBrowse.Size = new Size(80, 28);
                btnBrowse.Click += delegate { HandleBrowseVault(); };
                cardVault.Controls.Add(btnBrowse);

                // New Vault button (+3px top padding)
                ModernButton btnNew = new ModernButton();
                btnNew.Text = "New";
                btnNew.IconName = "plus";
                btnNew.IconSize = 12;
                btnNew.ShowFocusBorder = false;
                btnNew.Location = new Point(cardVault.Width - 72, 7);
                btnNew.Size = new Size(66, 28);
                btnNew.Click += delegate { HandleNewVault(); };
                cardVault.Controls.Add(btnNew);
            }

            contentPanel.Controls.Add(cardVault);
            curY += 54;

            // 2. Heading (Only in Create mode; in Unlock mode it was redundant with MASTER PASSWORD)
            if (mode == MasterPasswordMode.Create)
            {
                Label lblHeader = new Label();
                lblHeader.Text = "Create Master Password";
                lblHeader.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                lblHeader.ForeColor = ColorTextPrimary;
                lblHeader.Location = new Point(28, curY);
                lblHeader.AutoSize = true;
                contentPanel.Controls.Add(lblHeader);
                curY += 30; // Added 4px gap after Create Master Password (was 26)
            }

            // 3. Password Input Label (Properly spaced, no overlapping subtitle)
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
            passContainer.Size = new Size(contentPanel.Width - 56, 36);
            passContainer.BackColor = ColorSecondary;
            passContainer.TabIndex = 0;
            passContainer.Click += delegate { txtPassword.Focus(); };
            passContainer.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, passContainer.Width - 1, passContainer.Height - 1);
                }
            };

            txtPassword = new TextBox();
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.BackColor = ColorSecondary;
            txtPassword.ForeColor = ColorTextPrimary;
            txtPassword.Font = new Font("Segoe UI", 10f);
            txtPassword.Location = new Point(10, 8);
            txtPassword.Width = passContainer.Width - 48;
            txtPassword.PasswordChar = '●';
            txtPassword.TabIndex = 0;
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
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(10, 10, 16, 16), ColorTextMuted);
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
            curY += 42;

            if (mode == MasterPasswordMode.Create)
            {
                int cardW = contentPanel.Width - 56;

                // Password Strength Indicator (Right-aligned bar and label)
                Panel strengthWrapper = new Panel();
                strengthWrapper.Location = new Point(28, curY);
                strengthWrapper.Size = new Size(cardW, 20);
                strengthWrapper.BackColor = Color.Transparent;

                int barW = 84;

                strengthBar = new Panel();
                strengthBar.Size = new Size(barW, 4);
                strengthBar.BackColor = Color.Transparent;
                strengthBar.Paint += delegate(object s, PaintEventArgs pe)
                {
                    int segW = (strengthBar.Width - 9) / 4;
                    Color activeColor = (strengthScore >= 4) ? ColorSuccess :
                                        (strengthScore == 3) ? Color.FromArgb(56, 189, 248) :
                                        (strengthScore == 2) ? Color.FromArgb(245, 158, 11) :
                                        (strengthScore == 1) ? ColorDanger : WinColors.BorderSubtle;

                    for (int i = 0; i < 4; i++)
                    {
                        bool isFilled = (i < strengthScore);
                        using (SolidBrush b = new SolidBrush(isFilled ? activeColor : WinColors.BorderSubtle))
                        {
                            pe.Graphics.FillRectangle(b, i * (segW + 3), 0, segW, strengthBar.Height);
                        }
                    }
                };
                strengthWrapper.Controls.Add(strengthBar);

                lblStrength = new Label();
                lblStrength.AutoSize = true;
                lblStrength.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                lblStrength.ForeColor = ColorTextMuted;
                lblStrength.Text = "Strength: None";
                strengthWrapper.Controls.Add(lblStrength);

                contentPanel.Controls.Add(strengthWrapper);
                curY += 26;

                // Confirm Password Label
                Label lblConfirmLabel = new Label();
                lblConfirmLabel.Text = "CONFIRM MASTER PASSWORD *";
                lblConfirmLabel.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                lblConfirmLabel.ForeColor = ColorTextMuted;
                lblConfirmLabel.Location = new Point(28, curY);
                lblConfirmLabel.AutoSize = true;
                contentPanel.Controls.Add(lblConfirmLabel);
                curY += 18;

                // Confirm Password Container
                Panel confirmContainer = new Panel();
                confirmContainer.Location = new Point(28, curY);
                confirmContainer.Size = new Size(contentPanel.Width - 56, 36);
                confirmContainer.BackColor = ColorSecondary;
                confirmContainer.Paint += delegate(object s, PaintEventArgs pe)
                {
                    using (Pen p = new Pen(ColorBorder, 1f))
                    {
                        pe.Graphics.DrawRectangle(p, 0, 0, confirmContainer.Width - 1, confirmContainer.Height - 1);
                    }
                };

                txtConfirm = new TextBox();
                txtConfirm.BorderStyle = BorderStyle.None;
                txtConfirm.BackColor = ColorSecondary;
                txtConfirm.ForeColor = ColorTextPrimary;
                txtConfirm.Font = new Font("Segoe UI", 10f);
                txtConfirm.Location = new Point(10, 8);
                txtConfirm.Width = confirmContainer.Width - 20;
                txtConfirm.PasswordChar = '●';
                txtConfirm.TextChanged += delegate { lblError.Visible = false; };
                confirmContainer.Controls.Add(txtConfirm);

                contentPanel.Controls.Add(confirmContainer);
                curY += 44;
            }

            // Error Label
            lblError = new Label();
            lblError.Location = new Point(28, curY);
            lblError.Size = new Size(contentPanel.Width - 56, 18);
            lblError.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lblError.ForeColor = ColorDanger;
            lblError.Visible = false;
            contentPanel.Controls.Add(lblError);
            curY += 20;

            // Submit Button
            ModernButton btnSubmit = new ModernButton();
            btnSubmit.Text = (mode == MasterPasswordMode.Create) ? "Create Encrypted Vault" : "Unlock Vault";
            btnSubmit.IconName = (mode == MasterPasswordMode.Create) ? "plus" : "unlock";
            btnSubmit.IconSize = 14;
            btnSubmit.Location = new Point(28, curY);
            btnSubmit.Size = new Size(contentPanel.Width - 56, 38);
            btnSubmit.NormalBg = WinColors.Accent;
            btnSubmit.HoverBg = WinColors.AccentHover;
            btnSubmit.PressedBg = WinColors.AccentPressed;
            btnSubmit.BorderColor = WinColors.Accent;
            btnSubmit.NormalFg = Color.White;
            btnSubmit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSubmit.Click += delegate { HandleSubmit(); };
            contentPanel.Controls.Add(btnSubmit);
            curY += 48;

            // Secondary Action Buttons in Unlock Mode (only shown at initial startup, not in-app)
            if (!inAppContext && mode == MasterPasswordMode.Unlock)
            {
                Panel pnlAlt = new Panel();
                pnlAlt.Location = new Point(28, curY);
                pnlAlt.Size = new Size(contentPanel.Width - 56, 32);
                pnlAlt.BackColor = Color.Transparent;

                ModernButton btnAltOpen = new ModernButton();
                btnAltOpen.Text = "Open Other Vault";
                btnAltOpen.IconName = "folder";
                btnAltOpen.IconSize = 13;
                btnAltOpen.Location = new Point(0, 0);
                btnAltOpen.Size = new Size((pnlAlt.Width - 8) / 2, 32);
                btnAltOpen.Click += delegate { HandleBrowseVault(); };
                pnlAlt.Controls.Add(btnAltOpen);

                ModernButton btnAltNew = new ModernButton();
                btnAltNew.Text = "Create New Vault";
                btnAltNew.IconName = "plus";
                btnAltNew.IconSize = 13;
                btnAltNew.Location = new Point((pnlAlt.Width - 8) / 2 + 8, 0);
                btnAltNew.Size = new Size((pnlAlt.Width - 8) / 2, 32);
                btnAltNew.Click += delegate { HandleNewVault(); };
                pnlAlt.Controls.Add(btnAltNew);

                contentPanel.Controls.Add(pnlAlt);
                curY += 43; // Added 5px space between buttons and Exit Application
            }

            // Cancel / Exit Button
            if (inAppContext)
            {
                ModernButton btnCancel = new ModernButton();
                btnCancel.Text = "Cancel";
                btnCancel.Location = new Point(28, curY);
                btnCancel.Size = new Size(contentPanel.Width - 56, 32);
                btnCancel.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
                contentPanel.Controls.Add(btnCancel);
            }
            else
            {
                Label lblExit = new Label();
                lblExit.Text = "Exit Application";
                lblExit.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                lblExit.ForeColor = WinColors.TextSubtle;
                lblExit.Location = new Point(28, curY);
                lblExit.Size = new Size(contentPanel.Width - 56, 22);
                lblExit.TextAlign = ContentAlignment.MiddleCenter;
                lblExit.Cursor = Cursors.Hand;
                lblExit.MouseEnter += (s, e) => lblExit.ForeColor = WinColors.TextWhite;
                lblExit.MouseLeave += (s, e) => lblExit.ForeColor = WinColors.TextSubtle;
                lblExit.Click += delegate
                {
                    this.DialogResult = DialogResult.Abort;
                    this.Close();
                };
                contentPanel.Controls.Add(lblExit);
            }

            if (mode == MasterPasswordMode.Create)
            {
                UpdateStrength();
            }

            // Explicitly set ActiveControl and Focus so user can start typing immediately
            this.ActiveControl = txtPassword;
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
                strengthScore = 0;
                lblStrength.Text = "Strength: None";
                lblStrength.ForeColor = ColorTextMuted;
            }
            else
            {
                int score = 0;
                if (pwd.Length >= 6) score++;
                if (pwd.Length >= 10) score++;
                if (Regex.IsMatch(pwd, @"[A-Z]")) score++;
                if (Regex.IsMatch(pwd, @"[0-9]")) score++;
                if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

                if (score <= 1)
                {
                    strengthScore = 1;
                    lblStrength.Text = "Strength: Weak";
                    lblStrength.ForeColor = ColorDanger;
                }
                else if (score == 2)
                {
                    strengthScore = 2;
                    lblStrength.Text = "Strength: Medium";
                    lblStrength.ForeColor = Color.FromArgb(245, 158, 11);
                }
                else if (score == 3)
                {
                    strengthScore = 3;
                    lblStrength.Text = "Strength: Good";
                    lblStrength.ForeColor = Color.FromArgb(56, 189, 248);
                }
                else
                {
                    strengthScore = 4;
                    lblStrength.Text = "Strength: Strong";
                    lblStrength.ForeColor = ColorSuccess;
                }
            }

            // Align from right edge of cardW so label is never truncated or wrapped
            int cardW = contentPanel.Width - 56;
            int gap = 8;
            lblStrength.Location = new Point(cardW - lblStrength.PreferredWidth, 2);
            strengthBar.Location = new Point(lblStrength.Left - gap - strengthBar.Width, 7);

            strengthBar.Invalidate();
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
                    ActiveMasterPassword = password;
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
                    DecryptedSessionText = decrypted;
                    ActiveMasterPassword = password;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError("Incorrect master password. Please verify and try again.");
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
            if (keyData == (Keys.Control | Keys.O) && !inAppContext)
            {
                HandleBrowseVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.N) && !inAppContext)
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
