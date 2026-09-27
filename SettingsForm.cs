using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Dedicated Security & Settings Panel for KeyCraft.
    /// Manages Master Password updates, database re-encryption, vault security status,
    /// and immediate vault locking.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly CredentialService service;

        // Visual Palette (Obsidian Theme)
        private static readonly Color ColorBgApp = Color.FromArgb(15, 17, 23);
        private static readonly Color ColorBgCard = Color.FromArgb(20, 24, 33);
        private static readonly Color ColorBorder = Color.FromArgb(37, 44, 65);
        private static readonly Color ColorPrimary = Color.FromArgb(99, 102, 241);
        private static readonly Color ColorPrimaryHover = Color.FromArgb(129, 140, 248);
        private static readonly Color ColorTextPrimary = Color.FromArgb(243, 244, 246);
        private static readonly Color ColorTextMuted = Color.FromArgb(156, 163, 175);
        private static readonly Color ColorDanger = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorSuccess = Color.FromArgb(34, 197, 94);

        // UI Controls
        private TextBox txtCurrentPass;
        private TextBox txtNewPass;
        private TextBox txtConfirmPass;
        private Panel strengthBar;
        private Label lblStrength;
        private Label lblMessage;
        private bool isCurrentRevealed = false;
        private bool isNewRevealed = false;

        // Settings & Hotkey Controls
        private readonly AppSettings appSettings;
        private CheckBox chkStartup;
        private CheckBox chkCloseToTray;
        private CheckBox chkEnableHotkey;
        private HotkeyPickerControl pickerHotkey;
        private Label lblHotkeyStatus;
        private Label lblPrefsMessage;

        public event EventHandler VaultLockRequested;
        public event EventHandler SettingsSaved;

        public SettingsForm(CredentialService service, AppSettings settings = null)
        {
            this.service = service;
            this.appSettings = settings ?? AppSettings.Load();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ColorBgApp;
            this.ForeColor = ColorTextPrimary;
            this.Font = new Font("Segoe UI", 9.5f);
            this.Size = new Size(600, 740);
            this.KeyPreview = true;

            // Border painting
            this.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen borderPen = new Pen(ColorBorder, 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // Custom Title Drag Bar
            Panel titleBar = new Panel();
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

            // Gear Icon
            Panel iconPanel = new Panel();
            iconPanel.Location = new Point(14, 12);
            iconPanel.Size = new Size(16, 16);
            iconPanel.BackColor = Color.Transparent;
            iconPanel.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon("settings");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), ColorPrimaryHover);
                }
            };
            titleBar.Controls.Add(iconPanel);

            Label lblTitle = new Label();
            lblTitle.Text = "KeyCraft — Security & Settings";
            lblTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblTitle.ForeColor = ColorTextMuted;
            lblTitle.UseMnemonic = false;
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
            btnClose.Click += delegate { this.Close(); };
            titleBar.Controls.Add(btnClose);

            this.Controls.Add(titleBar);

            // Scrollable Content Container
            Panel scrollPanel = new Panel();
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            scrollPanel.Padding = new Padding(24, 16, 24, 16);
            this.Controls.Add(scrollPanel);
            scrollPanel.BringToFront();

            int curY = 10;

            // SECTION 1: MASTER PASSWORD MANAGEMENT CARD
            Panel cardMaster = CreateSectionCard(scrollPanel.Width - 48, 290);
            cardMaster.Location = new Point(24, curY);

            Label lblCardTitle = new Label();
            lblCardTitle.Text = "Master Password Management";
            lblCardTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblCardTitle.ForeColor = ColorTextPrimary;
            lblCardTitle.Location = new Point(16, 14);
            lblCardTitle.AutoSize = true;
            cardMaster.Controls.Add(lblCardTitle);

            Label lblCardDesc = new Label();
            lblCardDesc.Text = "Change the master password used to derive AES-256 keys and re-encrypt your database.";
            lblCardDesc.Font = new Font("Segoe UI", 8.5f);
            lblCardDesc.ForeColor = ColorTextMuted;
            lblCardDesc.Location = new Point(16, 38);
            lblCardDesc.Size = new Size(cardMaster.Width - 32, 38);
            cardMaster.Controls.Add(lblCardDesc);

            int passY = 78;

            // 1. Current Master Password
            Label lblCurr = new Label();
            lblCurr.Text = "CURRENT MASTER PASSWORD";
            lblCurr.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblCurr.ForeColor = ColorTextMuted;
            lblCurr.Location = new Point(16, passY);
            lblCurr.AutoSize = true;
            cardMaster.Controls.Add(lblCurr);
            passY += 18;

            Panel currContainer = CreateInputContainer(cardMaster.Width - 32, 34);
            currContainer.Location = new Point(16, passY);
            txtCurrentPass = CreateInnerTextBox(currContainer.Width - 44);
            currContainer.Controls.Add(txtCurrentPass);
            Button btnToggleCurr = CreateEyeToggle(delegate
            {
                isCurrentRevealed = !isCurrentRevealed;
                txtCurrentPass.PasswordChar = isCurrentRevealed ? '\0' : '●';
            });
            currContainer.Controls.Add(btnToggleCurr);
            cardMaster.Controls.Add(currContainer);
            passY += 40;

            // 2. New Master Password
            Label lblNew = new Label();
            lblNew.Text = "NEW MASTER PASSWORD";
            lblNew.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblNew.ForeColor = ColorTextMuted;
            lblNew.Location = new Point(16, passY);
            lblNew.AutoSize = true;
            cardMaster.Controls.Add(lblNew);
            passY += 18;

            Panel newContainer = CreateInputContainer(cardMaster.Width - 32, 34);
            newContainer.Location = new Point(16, passY);
            txtNewPass = CreateInnerTextBox(newContainer.Width - 44);
            txtNewPass.TextChanged += delegate { UpdateStrength(); };
            newContainer.Controls.Add(txtNewPass);
            Button btnToggleNew = CreateEyeToggle(delegate
            {
                isNewRevealed = !isNewRevealed;
                txtNewPass.PasswordChar = isNewRevealed ? '\0' : '●';
            });
            newContainer.Controls.Add(btnToggleNew);
            cardMaster.Controls.Add(newContainer);
            passY += 38;

            // Strength bar
            strengthBar = new Panel();
            strengthBar.Location = new Point(16, passY + 5);
            strengthBar.Size = new Size(160, 4);
            strengthBar.BackColor = Color.FromArgb(37, 44, 65);
            cardMaster.Controls.Add(strengthBar);

            lblStrength = new Label();
            lblStrength.Location = new Point(184, passY);
            lblStrength.Size = new Size(160, 16);
            lblStrength.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblStrength.ForeColor = ColorTextMuted;
            lblStrength.Text = "Strength: None";
            cardMaster.Controls.Add(lblStrength);
            passY += 22;

            // 3. Confirm New Password
            Label lblConf = new Label();
            lblConf.Text = "CONFIRM NEW PASSWORD";
            lblConf.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblConf.ForeColor = ColorTextMuted;
            lblConf.Location = new Point(16, passY);
            lblConf.AutoSize = true;
            cardMaster.Controls.Add(lblConf);
            passY += 18;

            Panel confContainer = CreateInputContainer(cardMaster.Width - 32, 34);
            confContainer.Location = new Point(16, passY);
            txtConfirmPass = CreateInnerTextBox(confContainer.Width - 16);
            confContainer.Controls.Add(txtConfirmPass);
            cardMaster.Controls.Add(confContainer);
            passY += 42;

            // Action Button & Feedback Label
            Button btnChangePass = new Button();
            btnChangePass.Text = "Update Master Password";
            btnChangePass.Location = new Point(16, passY);
            btnChangePass.Size = new Size(230, 36);
            btnChangePass.FlatStyle = FlatStyle.Flat;
            btnChangePass.FlatAppearance.BorderSize = 0;
            btnChangePass.BackColor = ColorPrimary;
            btnChangePass.ForeColor = Color.White;
            btnChangePass.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnChangePass.Cursor = Cursors.Hand;
            btnChangePass.Click += delegate { HandleChangePassword(); };
            cardMaster.Controls.Add(btnChangePass);

            lblMessage = new Label();
            lblMessage.Location = new Point(256, passY + 8);
            lblMessage.Size = new Size(cardMaster.Width - 266, 32);
            lblMessage.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblMessage.Visible = false;
            cardMaster.Controls.Add(lblMessage);

            cardMaster.Height = passY + 52;
            scrollPanel.Controls.Add(cardMaster);
            curY += cardMaster.Height + 16;

            // SECTION 2: GLOBAL ACTIVATION SHORTCUT (PowerToys Style)
            Panel cardHotkey = CreateSectionCard(scrollPanel.Width - 48, 195);
            cardHotkey.Location = new Point(24, curY);

            Label lblHotkeyTitle = new Label();
            lblHotkeyTitle.Text = "Global Activation Shortcut";
            lblHotkeyTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblHotkeyTitle.ForeColor = ColorTextPrimary;
            lblHotkeyTitle.Location = new Point(16, 14);
            lblHotkeyTitle.AutoSize = true;
            cardHotkey.Controls.Add(lblHotkeyTitle);

            Label lblHotkeyDesc = new Label();
            lblHotkeyDesc.Text = "Press this shortcut from anywhere in Windows to restore KeyCraft from the tray and focus search.";
            lblHotkeyDesc.Font = new Font("Segoe UI", 8.5f);
            lblHotkeyDesc.ForeColor = ColorTextMuted;
            lblHotkeyDesc.Location = new Point(16, 38);
            lblHotkeyDesc.Size = new Size(cardHotkey.Width - 32, 28);
            cardHotkey.Controls.Add(lblHotkeyDesc);

            chkEnableHotkey = new CheckBox();
            chkEnableHotkey.Text = "Enable System-Wide Global Hotkey";
            chkEnableHotkey.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            chkEnableHotkey.ForeColor = ColorTextPrimary;
            chkEnableHotkey.Location = new Point(16, 68);
            chkEnableHotkey.AutoSize = true;
            chkEnableHotkey.Checked = appSettings.HotkeyEnabled;
            chkEnableHotkey.CheckedChanged += delegate { UpdateHotkeyStatus(); };
            cardHotkey.Controls.Add(chkEnableHotkey);

            pickerHotkey = new HotkeyPickerControl();
            pickerHotkey.Location = new Point(16, 96);
            pickerHotkey.Size = new Size(320, 38);
            pickerHotkey.SetHotkey(appSettings.HotkeyModifiers, appSettings.HotkeyKey);
            pickerHotkey.HotkeyChanged += delegate { UpdateHotkeyStatus(); };
            cardHotkey.Controls.Add(pickerHotkey);

            Button btnResetHotkey = new Button();
            btnResetHotkey.Text = "Reset";
            btnResetHotkey.Location = new Point(344, 96);
            btnResetHotkey.Size = new Size(65, 38);
            btnResetHotkey.FlatStyle = FlatStyle.Flat;
            btnResetHotkey.FlatAppearance.BorderColor = ColorBorder;
            btnResetHotkey.BackColor = Color.FromArgb(24, 30, 46);
            btnResetHotkey.ForeColor = ColorTextPrimary;
            btnResetHotkey.Font = new Font("Segoe UI", 8.5f);
            btnResetHotkey.Cursor = Cursors.Hand;
            btnResetHotkey.Click += delegate
            {
                pickerHotkey.SetHotkey(GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, Keys.K);
                UpdateHotkeyStatus();
            };
            cardHotkey.Controls.Add(btnResetHotkey);

            Button btnSaveHotkey = new Button();
            btnSaveHotkey.Text = "Apply Hotkey";
            btnSaveHotkey.Location = new Point(416, 96);
            btnSaveHotkey.Size = new Size(110, 38);
            btnSaveHotkey.FlatStyle = FlatStyle.Flat;
            btnSaveHotkey.FlatAppearance.BorderSize = 0;
            btnSaveHotkey.BackColor = ColorPrimary;
            btnSaveHotkey.ForeColor = Color.White;
            btnSaveHotkey.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSaveHotkey.Cursor = Cursors.Hand;
            btnSaveHotkey.Click += delegate { HandleSaveHotkey(); };
            cardHotkey.Controls.Add(btnSaveHotkey);

            lblHotkeyStatus = new Label();
            lblHotkeyStatus.Location = new Point(16, 142);
            lblHotkeyStatus.Size = new Size(cardHotkey.Width - 32, 44);
            lblHotkeyStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblHotkeyStatus.UseMnemonic = false;
            cardHotkey.Controls.Add(lblHotkeyStatus);

            UpdateHotkeyStatus();

            cardHotkey.Height = 195;
            scrollPanel.Controls.Add(cardHotkey);
            curY += cardHotkey.Height + 16;

            // SECTION 3: APPLICATION BEHAVIOR & STARTUP
            Panel cardBehavior = CreateSectionCard(scrollPanel.Width - 48, 175);
            cardBehavior.Location = new Point(24, curY);

            Label lblBehaviorTitle = new Label();
            lblBehaviorTitle.Text = "Application Behavior & Startup";
            lblBehaviorTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblBehaviorTitle.ForeColor = ColorTextPrimary;
            lblBehaviorTitle.UseMnemonic = false;
            lblBehaviorTitle.Location = new Point(16, 14);
            lblBehaviorTitle.AutoSize = true;
            cardBehavior.Controls.Add(lblBehaviorTitle);

            Label lblBehaviorDesc = new Label();
            lblBehaviorDesc.Text = "Configure system startup integration and window title bar close behavior.";
            lblBehaviorDesc.Font = new Font("Segoe UI", 8.5f);
            lblBehaviorDesc.ForeColor = ColorTextMuted;
            lblBehaviorDesc.Location = new Point(16, 38);
            lblBehaviorDesc.Size = new Size(cardBehavior.Width - 32, 24);
            cardBehavior.Controls.Add(lblBehaviorDesc);

            chkStartup = new CheckBox();
            chkStartup.Text = "Start KeyCraft on Windows login (run silently in system tray)";
            chkStartup.Font = new Font("Segoe UI", 9f);
            chkStartup.ForeColor = ColorTextPrimary;
            chkStartup.Location = new Point(16, 68);
            chkStartup.Size = new Size(cardBehavior.Width - 32, 24);
            chkStartup.Checked = appSettings.RunOnStartup;
            cardBehavior.Controls.Add(chkStartup);

            chkCloseToTray = new CheckBox();
            chkCloseToTray.Text = "Close button (✕) minimizes to system tray instead of quitting";
            chkCloseToTray.Font = new Font("Segoe UI", 9f);
            chkCloseToTray.ForeColor = ColorTextPrimary;
            chkCloseToTray.Location = new Point(16, 96);
            chkCloseToTray.Size = new Size(cardBehavior.Width - 32, 24);
            chkCloseToTray.Checked = appSettings.CloseToTray;
            cardBehavior.Controls.Add(chkCloseToTray);

            Button btnSavePrefs = new Button();
            btnSavePrefs.Text = "Save Behavior Preferences";
            btnSavePrefs.Location = new Point(16, 128);
            btnSavePrefs.Size = new Size(200, 34);
            btnSavePrefs.FlatStyle = FlatStyle.Flat;
            btnSavePrefs.FlatAppearance.BorderSize = 0;
            btnSavePrefs.BackColor = ColorPrimary;
            btnSavePrefs.ForeColor = Color.White;
            btnSavePrefs.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnSavePrefs.Cursor = Cursors.Hand;
            btnSavePrefs.Click += delegate { HandleSaveBehavior(); };
            cardBehavior.Controls.Add(btnSavePrefs);

            lblPrefsMessage = new Label();
            lblPrefsMessage.Location = new Point(226, 134);
            lblPrefsMessage.Size = new Size(cardBehavior.Width - 236, 24);
            lblPrefsMessage.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblPrefsMessage.Visible = false;
            cardBehavior.Controls.Add(lblPrefsMessage);

            cardBehavior.Height = 175;
            scrollPanel.Controls.Add(cardBehavior);
            curY += cardBehavior.Height + 16;

            // SECTION 4: SECURITY & VAULT STATUS CARD
            Panel cardStatus = CreateSectionCard(scrollPanel.Width - 48, 140);
            cardStatus.Location = new Point(24, curY);

            Label lblStatusTitle = new Label();
            lblStatusTitle.Text = "Encryption & Vault Security";
            lblStatusTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblStatusTitle.ForeColor = ColorTextPrimary;
            lblStatusTitle.UseMnemonic = false;
            lblStatusTitle.Location = new Point(16, 14);
            lblStatusTitle.AutoSize = true;
            cardStatus.Controls.Add(lblStatusTitle);

            int infoY = 44;
            AddStatusRow(cardStatus, "Cipher Algorithm:", "AES-256-CBC (PKCS7)", infoY);
            infoY += 24;
            AddStatusRow(cardStatus, "Key Derivation:", "PBKDF2 HMAC-SHA1 (100,000 iterations)", infoY);
            infoY += 24;
            AddStatusRow(cardStatus, "Data Integrity:", "HMAC-SHA256 Constant-Time Token", infoY);
            infoY += 24;
            AddStatusRow(cardStatus, "Vault Status:", "● Active & Process-Locked", infoY, ColorSuccess);

            cardStatus.Height = infoY + 28;
            scrollPanel.Controls.Add(cardStatus);
            curY += cardStatus.Height + 16;

            // SECTION 3: SESSION ACTIONS
            Panel cardActions = CreateSectionCard(scrollPanel.Width - 48, 70);
            cardActions.Location = new Point(24, curY);

            Button btnLock = new Button();
            btnLock.Text = "       Lock Vault Now";
            btnLock.Location = new Point(16, 16);
            btnLock.Size = new Size(180, 36);
            btnLock.FlatStyle = FlatStyle.Flat;
            btnLock.FlatAppearance.BorderSize = 0;
            btnLock.BackColor = Color.FromArgb(220, 38, 38);
            btnLock.ForeColor = Color.White;
            btnLock.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnLock.Cursor = Cursors.Hand;
            btnLock.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap lockBmp = IconResources.GetIcon("lock");
                if (lockBmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, lockBmp, new Rectangle(14, 10, 16, 16), Color.White);
                }
            };
            btnLock.Click += delegate
            {
                if (VaultLockRequested != null)
                {
                    this.Close();
                    VaultLockRequested(this, EventArgs.Empty);
                }
            };
            cardActions.Controls.Add(btnLock);

            Button btnDone = new Button();
            btnDone.Text = "Done / Close";
            btnDone.Location = new Point(208, 16);
            btnDone.Size = new Size(120, 36);
            btnDone.FlatStyle = FlatStyle.Flat;
            btnDone.FlatAppearance.BorderColor = ColorBorder;
            btnDone.BackColor = Color.FromArgb(24, 30, 46);
            btnDone.ForeColor = ColorTextPrimary;
            btnDone.Font = new Font("Segoe UI", 9f);
            btnDone.Cursor = Cursors.Hand;
            btnDone.Click += delegate { this.Close(); };
            cardActions.Controls.Add(btnDone);

            scrollPanel.Controls.Add(cardActions);
        }

        private void AddStatusRow(Panel parent, string label, string value, int y, Color? valColor = null)
        {
            Label lbl = new Label();
            lbl.Text = label;
            lbl.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lbl.ForeColor = ColorTextMuted;
            lbl.UseMnemonic = false;
            lbl.Location = new Point(16, y);
            lbl.Size = new Size(130, 20);
            parent.Controls.Add(lbl);

            Label val = new Label();
            val.Text = value;
            val.Font = new Font("Segoe UI", 8.5f);
            val.ForeColor = valColor ?? ColorTextPrimary;
            val.UseMnemonic = false;
            val.Location = new Point(150, y);
            val.AutoSize = true;
            parent.Controls.Add(val);
        }

        private Panel CreateSectionCard(int width, int height)
        {
            Panel p = new Panel();
            p.Size = new Size(width, height);
            p.BackColor = ColorBgCard;
            p.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen pen = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };
            return p;
        }

        private Panel CreateInputContainer(int width, int height)
        {
            Panel p = new Panel();
            p.Size = new Size(width, height);
            p.BackColor = Color.FromArgb(17, 24, 39);
            p.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen pen = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };
            return p;
        }

        private TextBox CreateInnerTextBox(int width)
        {
            TextBox tb = new TextBox();
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = Color.FromArgb(17, 24, 39);
            tb.ForeColor = ColorTextPrimary;
            tb.Font = new Font("Segoe UI", 10f);
            tb.Location = new Point(8, 7);
            tb.Width = width;
            tb.PasswordChar = '●';
            return tb;
        }

        private Button CreateEyeToggle(Action onToggle)
        {
            Button btn = new Button();
            btn.Dock = DockStyle.Right;
            btn.Width = 32;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Cursor = Cursors.Hand;
            bool revealed = false;
            btn.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon(revealed ? "eye_off" : "eye");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(8, 9, 16, 16), ColorTextMuted);
                }
            };
            btn.Click += delegate
            {
                revealed = !revealed;
                if (onToggle != null) onToggle();
                btn.Invalidate();
            };
            return btn;
        }

        private void UpdateStrength()
        {
            if (strengthBar == null || lblStrength == null) return;
            string pwd = txtNewPass.Text;

            if (string.IsNullOrEmpty(pwd))
            {
                strengthBar.BackColor = Color.FromArgb(37, 44, 65);
                strengthBar.Width = 30;
                lblStrength.Text = "Strength: None";
                lblStrength.ForeColor = ColorTextMuted;
                return;
            }

            int score = 0;
            if (pwd.Length >= 6) score++;
            if (pwd.Length >= 10) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[A-Z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[0-9]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            if (score <= 1)
            {
                strengthBar.BackColor = ColorDanger;
                strengthBar.Width = 40;
                lblStrength.Text = "Strength: Weak";
                lblStrength.ForeColor = ColorDanger;
            }
            else if (score <= 3)
            {
                strengthBar.BackColor = Color.FromArgb(245, 158, 11);
                strengthBar.Width = 90;
                lblStrength.Text = "Strength: Medium";
                lblStrength.ForeColor = Color.FromArgb(245, 158, 11);
            }
            else
            {
                strengthBar.BackColor = ColorSuccess;
                strengthBar.Width = 160;
                lblStrength.Text = "Strength: Strong";
                lblStrength.ForeColor = ColorSuccess;
            }
        }

        private void HandleChangePassword()
        {
            string current = txtCurrentPass.Text;
            string newPass = txtNewPass.Text;
            string conf = txtConfirmPass.Text;

            if (string.IsNullOrEmpty(current))
            {
                ShowFeedback("Please enter your current master password.", false);
                txtCurrentPass.Focus();
                return;
            }
            if (string.IsNullOrEmpty(newPass) || newPass.Length < 6)
            {
                ShowFeedback("New master password must be at least 6 characters.", false);
                txtNewPass.Focus();
                return;
            }
            if (newPass != conf)
            {
                ShowFeedback("New passwords do not match.", false);
                txtConfirmPass.Focus();
                return;
            }

            string error;
            if (service.ChangeMasterPassword(current, newPass, out error))
            {
                ShowFeedback("✓ Master password updated & database re-encrypted!", true);
                txtCurrentPass.Text = string.Empty;
                txtNewPass.Text = string.Empty;
                txtConfirmPass.Text = string.Empty;
                UpdateStrength();
            }
            else
            {
                ShowFeedback("⚠ " + error, false);
                txtCurrentPass.SelectAll();
                txtCurrentPass.Focus();
            }
        }

        private void UpdateHotkeyStatus()
        {
            if (lblHotkeyStatus == null || pickerHotkey == null) return;

            if (chkEnableHotkey != null && !chkEnableHotkey.Checked)
            {
                lblHotkeyStatus.Text = "○ Global hotkey is currently disabled.";
                lblHotkeyStatus.ForeColor = ColorTextMuted;
                return;
            }

            if (pickerHotkey.HasConflict)
            {
                lblHotkeyStatus.Text = pickerHotkey.WarningMessage;
                lblHotkeyStatus.ForeColor = ColorDanger;
            }
            else
            {
                string combo = GlobalHotkeyManager.FormatHotkey(pickerHotkey.SelectedModifiers, pickerHotkey.SelectedKey);
                lblHotkeyStatus.Text = "✓ Shortcut is available & valid: [" + combo + "]";
                lblHotkeyStatus.ForeColor = ColorSuccess;
            }
        }

        private void HandleSaveHotkey()
        {
            appSettings.HotkeyEnabled = chkEnableHotkey.Checked;
            appSettings.HotkeyModifiers = pickerHotkey.SelectedModifiers;
            appSettings.HotkeyKey = pickerHotkey.SelectedKey;
            appSettings.Save();

            if (SettingsSaved != null)
            {
                SettingsSaved(this, EventArgs.Empty);
            }

            UpdateHotkeyStatus();
        }

        private void HandleSaveBehavior()
        {
            appSettings.RunOnStartup = chkStartup.Checked;
            appSettings.CloseToTray = chkCloseToTray.Checked;
            appSettings.Save();

            if (SettingsSaved != null)
            {
                SettingsSaved(this, EventArgs.Empty);
            }

            lblPrefsMessage.Text = "✓ Preferences saved successfully!";
            lblPrefsMessage.ForeColor = ColorSuccess;
            lblPrefsMessage.Visible = true;
        }

        private void ShowFeedback(string text, bool isSuccess)
        {
            lblMessage.Text = text;
            lblMessage.ForeColor = isSuccess ? ColorSuccess : ColorDanger;
            lblMessage.Visible = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
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
