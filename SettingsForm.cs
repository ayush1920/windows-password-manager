using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Screen 3: Security & Settings (KeyCraft — Security & Settings).
    /// Authentic Windows 11 WinUI 3 Dialog Modal directly replicating Stitch Screen 3
    /// with refined spacing, custom Fluent dark scroll bar, and non-cluttered user options.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly CredentialService service;
        private readonly AppSettings settings;

        // Custom Title Bar
        private Panel panelTitleBar;
        private TitleBarButton btnMin;
        private TitleBarButton btnClose;

        // Viewport & Custom Scroll
        private Panel panelViewport;
        private Panel panelContent;
        private DarkScrollBar customScrollBar;

        // Fields
        private TextBox txtCurrentPwd;
        private TextBox txtNewPwd;
        private TextBox txtConfirmPwd;
        private Label lblPwdStatus;
        private Panel pnlStrengthBar;
        private int newPwdStrengthScore = 0;

        // Password aliases
        private TextBox txtCurrentPass;
        private TextBox txtNewPass;
        private TextBox txtConfirmPass;
        private Label lblMessage;

        // Settings checkboxes
        private WinCheckbox chkHotkey;
        private WinCheckbox chkEnableHotkey;
        private WinCheckbox chkStartLogin;
        private WinCheckbox chkStartup;
        private WinCheckbox chkMinimizeTray;
        private WinCheckbox chkCloseToTray;

        // Hotkey picker control
        private HotkeyPickerControl pickerHotkey;

#pragma warning disable 0067
        // Public events
        public event EventHandler RequestLockVault;
        public event EventHandler SettingsSaved;
#pragma warning restore 0067

        private void UpdateNewPasswordStrength(string pwd)
        {
            if (string.IsNullOrEmpty(pwd))
            {
                newPwdStrengthScore = 0;
                if (lblPwdStatus != null)
                {
                    lblPwdStatus.Text = "None";
                    lblPwdStatus.ForeColor = WinColors.TextMuted;
                }
                return;
            }

            int score = 0;
            if (pwd.Length >= 6) score++;
            if (pwd.Length >= 10) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[A-Z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[0-9]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            if (lblPwdStatus != null)
            {
                if (score <= 1)
                {
                    newPwdStrengthScore = 1;
                    lblPwdStatus.Text = "Weak";
                    lblPwdStatus.ForeColor = WinColors.WeakText;
                }
                else if (score <= 3)
                {
                    newPwdStrengthScore = 2;
                    lblPwdStatus.Text = "Medium";
                    lblPwdStatus.ForeColor = WinColors.MediumText;
                }
                else
                {
                    newPwdStrengthScore = 4;
                    lblPwdStatus.Text = "Strong (128-bit)";
                    lblPwdStatus.ForeColor = WinColors.SuccessLight;
                }
            }
        }

        public SettingsForm(CredentialService credService) : this(credService, null)
        {
        }

        public SettingsForm(CredentialService credService, AppSettings appSettings)
        {
            if (credService == null) throw new ArgumentNullException("credService");
            this.service = credService;
            this.settings = appSettings ?? AppSettings.Load();

            InitializeComponent();
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
        }

        private void InitializeComponent()
        {
            this.Text = "KeyCraft — Security & Settings";
            this.Size = new Size(620, 760);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = WinColors.Window;
            this.ForeColor = WinColors.TextWhite;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            // Form border painting
            this.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
            this.Resize += delegate { this.Invalidate(); };

            // ==========================================
            // 1. TITLE BAR (Height 36)
            // ==========================================
            panelTitleBar = new Panel();
            panelTitleBar.Dock = DockStyle.Top;
            panelTitleBar.Height = 36;
            panelTitleBar.BackColor = WinColors.Window;
            panelTitleBar.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);

            Panel pnlIcon = new Panel();
            pnlIcon.Location = new Point(14, 10);
            pnlIcon.Size = new Size(16, 16);
            pnlIcon.BackColor = Color.Transparent;
            pnlIcon.Paint += delegate (object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon("shield");
                if (bmp != null) IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), WinColors.Accent);
            };
            pnlIcon.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            panelTitleBar.Controls.Add(pnlIcon);

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "KeyCraft — Security & Settings";
            lblTitle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblTitle.ForeColor = WinColors.TextSecondary;
            lblTitle.Location = new Point(36, 9);
            lblTitle.AutoSize = true;
            lblTitle.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            panelTitleBar.Controls.Add(lblTitle);

            // Caption Buttons Container
            Panel pnlCaptions = new Panel();
            pnlCaptions.Dock = DockStyle.Right;
            pnlCaptions.Size = new Size(92, 36);

            btnMin = new TitleBarButton(TitleButtonType.Minimize);
            btnMin.Location = new Point(0, 0);
            btnMin.Size = new Size(46, 36);
            btnMin.Click += delegate { this.WindowState = FormWindowState.Minimized; };
            pnlCaptions.Controls.Add(btnMin);

            btnClose = new TitleBarButton(TitleButtonType.Close);
            btnClose.Location = new Point(46, 0);
            btnClose.Size = new Size(46, 36);
            btnClose.Click += delegate { this.Close(); };
            pnlCaptions.Controls.Add(btnClose);

            panelTitleBar.Controls.Add(pnlCaptions);

            Panel pnlTitleDivider = new Panel();
            pnlTitleDivider.Dock = DockStyle.Bottom;
            pnlTitleDivider.Height = 1;
            pnlTitleDivider.BackColor = WinColors.BorderSubtle;
            panelTitleBar.Controls.Add(pnlTitleDivider);

            // ==========================================
            // 2. STICKY FOOTER COMMAND BAR (Height 56)
            // End-to-end division matching Title Bar, with comfortable 12px vertical spacing
            // ==========================================
            Panel panelFooter = new Panel();
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Height = 56;
            panelFooter.BackColor = WinColors.Chrome;
            panelFooter.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.BorderSubtle, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, panelFooter.Width, 0);
                }
            };

            // Two distinct buttons on the right: Close (Secondary) and Done (Primary Accent)
            ModernButton btnCloseFooter = new ModernButton();
            btnCloseFooter.Text = "Close";
            btnCloseFooter.Size = new Size(84, 32);
            btnCloseFooter.Location = new Point(panelFooter.ClientSize.Width - 16 - 84 - 10 - 84, 12);
            btnCloseFooter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCloseFooter.NormalBg = WinColors.BtnSecondary;
            btnCloseFooter.HoverBg = WinColors.BtnSecondaryHover;
            btnCloseFooter.BorderColor = WinColors.Border;
            btnCloseFooter.NormalFg = WinColors.TextWhite;
            btnCloseFooter.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            panelFooter.Controls.Add(btnCloseFooter);

            ModernButton btnDone = new ModernButton();
            btnDone.Text = "Done";
            btnDone.Size = new Size(84, 32);
            btnDone.Location = new Point(panelFooter.ClientSize.Width - 16 - 84, 12);
            btnDone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDone.NormalBg = WinColors.Accent;
            btnDone.HoverBg = WinColors.AccentHover;
            btnDone.PressedBg = WinColors.AccentPressed;
            btnDone.BorderColor = WinColors.Accent;
            btnDone.NormalFg = Color.White;
            btnDone.Click += delegate
            {
                HandleSaveHotkey();
                HandleSaveBehavior();
                if (settings != null) settings.Save();
                if (SettingsSaved != null) SettingsSaved(this, EventArgs.Empty);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            panelFooter.Controls.Add(btnDone);

            // ==========================================
            // 3. MAIN SCROLLABLE VIEWPORT WITH CUSTOM DARK SCROLLBAR
            // ==========================================
            panelViewport = new Panel();
            panelViewport.Dock = DockStyle.Fill;
            panelViewport.BackColor = WinColors.Window;
            panelViewport.AutoScroll = false; // No ugly Win32 white scrollbars!

            customScrollBar = new DarkScrollBar();
            customScrollBar.Dock = DockStyle.Right;
            customScrollBar.Width = 8;
            customScrollBar.Visible = false;
            customScrollBar.ValueChanged += delegate
            {
                if (panelContent != null)
                {
                    panelContent.Top = -customScrollBar.Value;
                }
            };
            panelViewport.Controls.Add(customScrollBar);

            panelContent = new Panel();
            panelContent.Location = new Point(0, 0);
            panelContent.Width = 620;
            panelContent.BackColor = WinColors.Window;
            panelViewport.Controls.Add(panelContent);

            // Enable mouse wheel scrolling
            MouseEventHandler wheelHandler = delegate (object s, MouseEventArgs e)
            {
                if (customScrollBar.Visible)
                {
                    customScrollBar.Value -= (e.Delta / 3);
                }
            };
            panelViewport.MouseWheel += wheelHandler;
            panelContent.MouseWheel += wheelHandler;

            int cardWidth = 588;
            int curY = 12;

            // ------------------------------------------
            // GROUP 1: Master Password Management
            // ------------------------------------------
            Panel card1 = CreateCard(cardWidth, 352, curY);
            panelContent.Controls.Add(card1);
            card1.MouseWheel += wheelHandler;
            curY += 366;

            bool hasActiveVault = service != null && service.HasActiveVault;
            string vaultCardDesc = hasActiveVault
                ? "Change the master password used to derive AES-256 keys and re-encrypt the vault."
                : "No vault currently loaded. Open or create an encrypted vault to manage its master password.";
            AddCardHeader(card1, "key", "Master Password Management", vaultCardDesc, hasActiveVault ? (Color?)null : WinColors.TextSubtle);

            // Current Password
            Label lblCurr = CreateFieldLabel("CURRENT MASTER PASSWORD", 16, 70);
            card1.Controls.Add(lblCurr);
            txtCurrentPwd = CreateInputBox(card1, 16, 90, cardWidth - 32, true);
            txtCurrentPwd.Enabled = hasActiveVault;

            // New Password
            Label lblNew = CreateFieldLabel("NEW MASTER PASSWORD", 16, 132);
            card1.Controls.Add(lblNew);
            txtNewPwd = CreateInputBox(card1, 16, 152, cardWidth - 32, true);
            txtNewPwd.Enabled = hasActiveVault;

            // Strength bar
            Panel pnlStrength = new Panel();
            pnlStrength.Location = new Point(16, 192);
            pnlStrength.Size = new Size(cardWidth - 32, 16);
            pnlStrength.BackColor = Color.Transparent;

            Label lblStrTag = new Label();
            lblStrTag.Text = "Strength:";
            lblStrTag.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblStrTag.ForeColor = WinColors.TextMuted;
            lblStrTag.Location = new Point(0, 0);
            lblStrTag.AutoSize = true;
            pnlStrength.Controls.Add(lblStrTag);

            lblPwdStatus = new Label();
            lblPwdStatus.Text = "None";
            lblPwdStatus.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblPwdStatus.ForeColor = WinColors.TextMuted;
            lblPwdStatus.Dock = DockStyle.Right;
            lblPwdStatus.AutoSize = true;
            pnlStrength.Controls.Add(lblPwdStatus);

            card1.Controls.Add(pnlStrength);

            // 4 dynamic segments matching MasterPasswordForm
            pnlStrengthBar = new Panel();
            pnlStrengthBar.Location = new Point(16, 212);
            pnlStrengthBar.Size = new Size(cardWidth - 32, 4);
            pnlStrengthBar.BackColor = Color.Transparent;
            pnlStrengthBar.Paint += delegate (object s, PaintEventArgs pe)
            {
                int segW = (pnlStrengthBar.Width - 9) / 4;
                Color activeColor = (newPwdStrengthScore >= 4) ? WinColors.Success :
                                    (newPwdStrengthScore >= 2) ? WinColors.MediumText :
                                    (newPwdStrengthScore == 1) ? WinColors.WeakText : WinColors.BorderSubtle;

                for (int i = 0; i < 4; i++)
                {
                    bool isFilled = (newPwdStrengthScore >= 4) ||
                                    (newPwdStrengthScore >= 2 && i < 2) ||
                                    (newPwdStrengthScore == 1 && i < 1);
                    Color segColor = isFilled ? activeColor : WinColors.BorderSubtle;
                    using (SolidBrush b = new SolidBrush(segColor))
                    {
                        pe.Graphics.FillRectangle(b, i * (segW + 3), 0, segW, 4);
                    }
                }
            };
            card1.Controls.Add(pnlStrengthBar);

            txtNewPwd.TextChanged += delegate
            {
                UpdateNewPasswordStrength(txtNewPwd.Text);
                pnlStrengthBar.Invalidate();
            };

            // Confirm Password (with extra spacing after strength)
            Label lblConf = CreateFieldLabel("CONFIRM NEW PASSWORD", 16, 236);
            card1.Controls.Add(lblConf);
            txtConfirmPwd = CreateInputBox(card1, 16, 256, cardWidth - 32, false);
            txtConfirmPwd.Enabled = hasActiveVault;

            txtCurrentPass = txtCurrentPwd;
            txtNewPass = txtNewPwd;
            txtConfirmPass = txtConfirmPwd;

            // Update Master Password Button (with generous spacing after text box)
            ModernButton btnUpdatePwd = new ModernButton();
            btnUpdatePwd.Text = "Update Master Password";
            btnUpdatePwd.IconName = "refresh";
            btnUpdatePwd.IconSize = 13;
            btnUpdatePwd.NormalBg = WinColors.Accent;
            btnUpdatePwd.HoverBg = WinColors.AccentHover;
            btnUpdatePwd.PressedBg = WinColors.AccentPressed;
            btnUpdatePwd.BorderColor = WinColors.Accent;
            btnUpdatePwd.NormalFg = Color.White;
            btnUpdatePwd.Size = new Size(220, 32);
            btnUpdatePwd.Location = new Point(16, 304);
            btnUpdatePwd.Enabled = hasActiveVault;
            btnUpdatePwd.Click += delegate
            {
                if (service == null || !service.HasActiveVault)
                {
                    MessageBox.Show(this, "No vault is currently loaded. Please open or create a vault first.", "No Active Vault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string err;
                bool ok = service.ChangeMasterPassword(txtCurrentPwd.Text, txtNewPwd.Text, txtConfirmPwd.Text, out err);
                if (ok)
                {
                    MessageBox.Show(this, "Master password updated and vault re-encrypted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtCurrentPwd.Text = string.Empty;
                    txtNewPwd.Text = string.Empty;
                    txtConfirmPwd.Text = string.Empty;
                    UpdateNewPasswordStrength(string.Empty);
                    pnlStrengthBar.Invalidate();
                }
                else
                {
                    MessageBox.Show(this, err, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            card1.Controls.Add(btnUpdatePwd);

            lblMessage = new Label();
            lblMessage.AutoSize = true;
            lblMessage.Location = new Point(245, 312);
            lblMessage.ForeColor = WinColors.Success;
            lblMessage.Visible = false;
            card1.Controls.Add(lblMessage);

            // ------------------------------------------
            // GROUP 2: Global Activation Shortcut
            // ------------------------------------------
            // ------------------------------------------
            // GROUP 2: Global Hotkey
            // ------------------------------------------
            Panel card2 = CreateCard(cardWidth, 180, curY);
            panelContent.Controls.Add(card2);
            card2.MouseWheel += wheelHandler;
            curY += 194;

            AddCardHeader(card2, "keyboard", "Global Activation Shortcut", "Press this shortcut from anywhere in Windows to restore KeyCraft from system tray.");

            chkHotkey = new WinCheckbox();
            chkHotkey.Text = "Enable System-Wide Global Hotkey";
            chkHotkey.Checked = (settings != null) ? settings.HotkeyEnabled : true;
            chkHotkey.Location = new Point(16, 70);
            chkHotkey.Size = new Size(320, 20);
            chkHotkey.CheckedChanged += delegate
            {
                HandleSaveHotkey();
            };
            card2.Controls.Add(chkHotkey);
            chkEnableHotkey = chkHotkey;

            pickerHotkey = new HotkeyPickerControl();
            pickerHotkey.Location = new Point(16, 96);
            pickerHotkey.Size = new Size(cardWidth - 32, 38);
            if (settings != null)
            {
                pickerHotkey.SetHotkey(settings.HotkeyModifiers, settings.HotkeyKey);
            }
            pickerHotkey.HotkeyChanged += delegate
            {
                HandleSaveHotkey();
            };
            card2.Controls.Add(pickerHotkey);

            // Reset to default button
            ModernButton btnResetHot = new ModernButton();
            btnResetHot.Text = "Reset to Default (Ctrl+Alt+K)";
            btnResetHot.Size = new Size(180, 26);
            btnResetHot.Location = new Point(16, 142);
            btnResetHot.Click += delegate
            {
                chkHotkey.Checked = true;
                if (pickerHotkey != null)
                {
                    pickerHotkey.SetHotkey(0x0001 | 0x0002, Keys.K);
                }
                HandleSaveHotkey();
            };
            card2.Controls.Add(btnResetHot);

            // ------------------------------------------
            // GROUP 3: Application Behavior & Startup
            // Auto-saving options without clutter
            // ------------------------------------------
            Panel card3 = CreateCard(cardWidth, 136, curY);
            panelContent.Controls.Add(card3);
            card3.MouseWheel += wheelHandler;
            curY += 150;

            AddCardHeader(card3, "settings", "Application Behavior & Startup", "Configure system startup integration and window title bar close behavior.");

            chkStartLogin = new WinCheckbox();
            chkStartLogin.Text = "Start KeyCraft on Windows login (run silently in system tray)";
            chkStartLogin.Checked = (settings != null) ? settings.RunOnStartup : false;
            chkStartLogin.Location = new Point(16, 70);
            chkStartLogin.Size = new Size(480, 20);
            chkStartLogin.CheckedChanged += delegate
            {
                if (settings != null)
                {
                    settings.RunOnStartup = chkStartLogin.Checked;
                    settings.Save();
                }
            };
            card3.Controls.Add(chkStartLogin);
            chkStartup = chkStartLogin;

            chkMinimizeTray = new WinCheckbox();
            chkMinimizeTray.Text = "Close button (✕) minimizes to system tray instead of quitting";
            chkMinimizeTray.Checked = (settings != null) ? settings.CloseToTray : false;
            chkMinimizeTray.Location = new Point(16, 98);
            chkMinimizeTray.Size = new Size(480, 20);
            chkMinimizeTray.CheckedChanged += delegate
            {
                if (settings != null)
                {
                    settings.CloseToTray = chkMinimizeTray.Checked;
                    settings.Save();
                }
            };
            card3.Controls.Add(chkMinimizeTray);
            chkCloseToTray = chkMinimizeTray;

            // Finalize panelContent height
            panelContent.Height = curY + 12;

            // Viewport resize layout for custom scrollbar
            panelViewport.Resize += delegate
            {
                int vpW = panelViewport.ClientSize.Width;
                int vpH = panelViewport.ClientSize.Height;

                bool needScroll = panelContent.Height > vpH;
                customScrollBar.Visible = needScroll;
                customScrollBar.Height = vpH;
                customScrollBar.ViewSize = vpH;
                customScrollBar.Maximum = panelContent.Height;

                panelContent.Width = vpW - (needScroll ? 10 : 0);
            };

            // Assemble Form (WinForms docking: Controls added earlier dock inside controls added later)
            // 1. Fill viewport added first so it fills between top and bottom
            this.Controls.Add(panelViewport);
            // 2. Edge-to-edge footer docked to form bottom
            this.Controls.Add(panelFooter);
            // 3. Edge-to-edge title bar docked to form top
            this.Controls.Add(panelTitleBar);
        }

        private Panel CreateCard(int w, int h, int y)
        {
            Panel card = new Panel();
            card.Size = new Size(w, h);
            card.Location = new Point(16, y);
            card.BackColor = WinColors.Card;
            card.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            return card;
        }

        private void AddCardHeader(Panel card, string iconName, string title, string sub, Color? iconTint = null)
        {
            Color tint = iconTint.HasValue ? iconTint.Value : WinColors.Accent;
            Panel pnlIcon = new Panel();
            pnlIcon.Location = new Point(16, 14);
            pnlIcon.Size = new Size(16, 16);
            pnlIcon.BackColor = Color.Transparent;
            pnlIcon.Paint += delegate (object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon(iconName);
                if (bmp != null) IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), tint);
            };
            card.Controls.Add(pnlIcon);

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblTitle.ForeColor = WinColors.TextWhite;
            lblTitle.Location = new Point(38, 12);
            lblTitle.AutoSize = true;
            card.Controls.Add(lblTitle);

            // Generous spacing between title and description
            Label lblSub = new Label();
            lblSub.Text = sub;
            lblSub.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            lblSub.ForeColor = WinColors.TextMuted;
            lblSub.Location = new Point(16, 38);
            lblSub.Size = new Size(card.Width - 32, 18);
            card.Controls.Add(lblSub);
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lbl.ForeColor = WinColors.TextSecondary;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private TextBox CreateInputBox(Panel parent, int x, int y, int w, bool hasEye)
        {
            Panel pnl = new Panel();
            pnl.Location = new Point(x, y);
            pnl.Size = new Size(w, 28);
            pnl.BackColor = WinColors.InputBg;

            TextBox tb = new TextBox();
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = WinColors.InputBg;
            tb.ForeColor = WinColors.TextWhite;
            tb.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            tb.UseSystemPasswordChar = true;
            tb.Location = new Point(8, 6);
            tb.Size = new Size(hasEye ? w - 38 : w - 16, 18);

            pnl.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(tb.Focused ? WinColors.Accent : WinColors.Border, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            tb.GotFocus += (s, e) => pnl.Invalidate();
            tb.LostFocus += (s, e) => pnl.Invalidate();
            pnl.Controls.Add(tb);

            if (hasEye)
            {
                ModernButton eye = new ModernButton();
                eye.Size = new Size(26, 22);
                eye.Location = new Point(w - 29, 3);
                eye.NormalBg = Color.Transparent;
                eye.HoverBg = WinColors.BtnSecondaryHover;
                eye.BorderColor = Color.Transparent;
                eye.IconName = "eye";
                eye.IconSize = 13;
                bool rev = false;
                eye.Click += delegate
                {
                    rev = !rev;
                    tb.UseSystemPasswordChar = !rev;
                    eye.IconName = rev ? "eye_off" : "eye";
                    eye.Invalidate();
                };
                pnl.Controls.Add(eye);
            }

            parent.Controls.Add(pnl);
            return tb;
        }

        private Label CreateKeyBadge(string text, int x, int y, int w, bool isKeyK)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Consolas", 8f, FontStyle.Bold);
            lbl.ForeColor = Color.White;
            lbl.BackColor = isKeyK ? Color.FromArgb(0, 72, 131) : Color.FromArgb(42, 42, 42);
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(w, 18);
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(isKeyK ? WinColors.Accent : Color.FromArgb(68, 68, 68), 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, lbl.Width - 1, lbl.Height - 1);
                }
            };
            return lbl;
        }

        private Label CreatePlusLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(119, 119, 119);
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(10, 16);
            return lbl;
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

        private void HandleSaveBehavior()
        {
            if (settings != null)
            {
                if (chkStartup != null) settings.RunOnStartup = chkStartup.Checked;
                if (chkCloseToTray != null) settings.CloseToTray = chkCloseToTray.Checked;
                settings.Save();
            }
        }

        private void HandleSaveHotkey()
        {
            if (settings != null)
            {
                if (chkEnableHotkey != null) settings.HotkeyEnabled = chkEnableHotkey.Checked;
                if (pickerHotkey != null)
                {
                    settings.HotkeyModifiers = pickerHotkey.CurrentModifiers;
                    settings.HotkeyKey = pickerHotkey.CurrentKey;
                }
                settings.Save();
            }
        }

        private void HandleChangePassword()
        {
            if (lblMessage == null) lblMessage = new Label();
            if (service == null || !service.HasActiveVault)
            {
                lblMessage.Text = "No vault loaded.";
                lblMessage.Visible = true;
                return;
            }
            string cur = txtCurrentPass != null ? txtCurrentPass.Text : (txtCurrentPwd != null ? txtCurrentPwd.Text : "");
            string np = txtNewPass != null ? txtNewPass.Text : (txtNewPwd != null ? txtNewPwd.Text : "");
            string cp = txtConfirmPass != null ? txtConfirmPass.Text : (txtConfirmPwd != null ? txtConfirmPwd.Text : "");
            string err;
            bool ok = service.ChangeMasterPassword(cur, np, cp, out err);
            if (ok)
            {
                lblMessage.Text = "Master password successfully updated!";
                lblMessage.Visible = true;
            }
            else
            {
                lblMessage.Text = "Error: " + err;
                lblMessage.Visible = true;
            }
        }
    }
}
