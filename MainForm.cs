using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Screen 2: Main Vault Manager (KeyCraft — [vault.kcrypt]).
    /// Authentic Windows 11 Fluent two-column desktop application replicating Stitch Screen 2.
    /// </summary>
    public class MainForm : Form, ISingleInstanceTarget
    {
        private readonly CredentialService service;
        private List<Credential> cachedList = new List<Credential>();
        private Credential selectedCredential = null;
        private int currentSelectedIndex = -1;

        // Custom Title Bar Controls
        private Panel panelTitleBar;
        private Label lblAppTitle;
        private Label lblVaultBadge;
        private TitleBarButton btnMin;
        private TitleBarButton btnMax;
        private TitleBarButton btnClose;

        // CommandBar Toolbar Controls
        private Panel panelCommandBar;
        private ModernButton btnNewVault;
        private ModernButton btnOpenVault;
        private ModernButton btnLockVault;
        private ModernButton btnRefresh;
        private ModernButton btnSettings;

        // Workspace Controls
        private Panel panelWorkspace;

        // Left DataGrid / Master List
        private Panel panelLeft;
        private TextBox txtSearch;
        private ModernButton btnClearSearch;
        private ModernComboBox cboFilter;
        private ListView lvCredentials;
        private bool isAdjustingColumns = false;
        private ModernButton btnCopyPassword;
        private ModernButton btnCopyUsername;
        private ModernButton btnDelete;
        private Label lblTotalCount;

        // Table Empty State Watermark Overlay
        private Panel pnlTableOverlay;
        private Panel pnlOverlayCard;
        private Label lblOverlayTitle;
        private Label lblOverlaySub;
        private ModernButton btnOverlayNew;
        private ModernButton btnOverlayOpen;
        private ModernButton btnOverlayAdd;

        // Clipboard Security Auto-Clear Timer
        private Timer clipboardClearTimer;
        private string lastCopiedPassword = null;

        // Right Inspector Pane
        private Panel panelRight;
        private Label lblEditorTitle;
        private TextBox txtSlNo;
        private Label lblSlNoTotal;
        private ModernButton btnStepUp;
        private ModernButton btnStepDown;
        private TextBox txtService;
        private TextBox txtUsername;
        private ModernButton btnCopyUserField;
        private TextBox txtPassword;
        private Label lblStrength;
        private ModernButton btnTogglePassword;
        private ModernButton btnGeneratePassword;
        private TextBox txtNotes;
        private Label lblLastModified;
        private Label lblRecordId;
        private ModernButton btnSave;
        private ModernButton btnClearForm;

        // Status Bar
        private Panel panelStatusBar;
        private Label lblStatusRecords;

        // Compatibility & Test Automation Aliases
        private TextBox txtSerialNo;
        private ModernButton btnClear;
        private ModernButton btnMoveUp;
        private ModernButton btnMoveDown;
        private ComboBox cmbSearchColumn;
        private Label lblToast;
        private NotifyIcon notifyIcon;
        private KeyboardShortcutManager shortcutManager;

        private bool isPasswordRevealed = false;
        private bool promptNewVaultOnShown = false;
        private bool isPromptingVaultAction = false;
        private NotifyIcon trayIcon;
        private WindowResizeFilter resizeFilter;

        private GlobalHotkeyManager hotkeyManager;
        private bool hasShownTrayTip = false;
        private ContextMenuStrip trayMenu;
        private FormWindowState previousWindowState = FormWindowState.Normal;
        private bool isMinimizedToTray = false;

        public MainForm(CredentialService credService, bool isNewVaultRequested = false)
        {
            if (credService == null) throw new ArgumentNullException("credService");
            this.service = credService;
            this.promptNewVaultOnShown = isNewVaultRequested;

            hotkeyManager = new GlobalHotkeyManager();
            hotkeyManager.HotkeyPressed += delegate
            {
                if (isMinimizedToTray)
                {
                    RestoreFromTray(false);
                }
                else
                {
                    MinimizeToTray();
                }
            };

            InitializeComponent();
            InitializeTrayIcon();
            SingleInstanceController.RegisterActiveForm(this);
            LoadCredentials();

            shortcutManager = new KeyboardShortcutManager();
            shortcutManager.DoubleEscapeTriggered = delegate { MinimizeToTray(); };
            shortcutManager.SingleEscapeTriggered = delegate { HandleSingleEscape(); };
            shortcutManager.MoveUpTriggered = delegate { MoveSelectedItem(-1); };
            shortcutManager.MoveDownTriggered = delegate { MoveSelectedItem(1); };
            shortcutManager.CopyPasswordTriggered = delegate { CopySelectedPassword(); };
            shortcutManager.FocusSearchTriggered = delegate { if (txtSearch != null) txtSearch.Focus(); };
            shortcutManager.SaveCredentialTriggered = delegate { SaveOrUpdateCredential(); };
            shortcutManager.ClearFormTriggered = delegate { RevertToNewCredential(); };
            shortcutManager.TogglePasswordTriggered = delegate
            {
                if (btnTogglePassword != null) btnTogglePassword.PerformClick();
            };
            shortcutManager.GeneratePasswordTriggered = delegate
            {
                if (btnGeneratePassword != null) btnGeneratePassword.PerformClick();
            };
            shortcutManager.OpenSettingsTriggered = delegate { OpenSettingsDialog(); };
            shortcutManager.RefreshTriggered = delegate { LoadCredentials(); };
            shortcutManager.DeleteTriggered = delegate { DeleteSelectedCredential(); };

            this.Shown += delegate
            {
                if (promptNewVaultOnShown)
                {
                    promptNewVaultOnShown = false;
                    this.BeginInvoke(new Action(delegate { PromptCreateNewVault(); }));
                }
                else
                {
                    this.BeginInvoke(new Action(delegate
                    {
                        if (txtSearch != null && txtSearch.Enabled)
                        {
                            txtSearch.Focus();
                            txtSearch.SelectAll();
                        }
                    }));
                }
            };
        }

        private void InitializeTrayIcon()
        {
            try
            {
                trayMenu = new ContextMenuStrip();
                trayMenu.BackColor = WinColors.Card;
                trayMenu.ForeColor = WinColors.TextWhite;
                trayMenu.RenderMode = ToolStripRenderMode.System;

                ToolStripMenuItem itemOpen = new ToolStripMenuItem("Open KeyCraft", null, delegate { RestoreFromTray(false); });
                itemOpen.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                itemOpen.ForeColor = WinColors.TextWhite;

                ToolStripMenuItem itemSearch = new ToolStripMenuItem("Search Credentials (Ctrl+F)", null, delegate
                {
                    RestoreFromTray(false);
                    if (txtSearch != null)
                    {
                        txtSearch.Focus();
                        txtSearch.SelectAll();
                    }
                });
                itemSearch.ForeColor = WinColors.TextWhite;

                ToolStripMenuItem itemLock = new ToolStripMenuItem("Lock Vault (Ctrl+L)", null, delegate
                {
                    RestoreFromTray(false);
                    LockAndShowUnlockScreen();
                });
                itemLock.ForeColor = WinColors.TextWhite;

                ToolStripMenuItem itemSettings = new ToolStripMenuItem("Settings...", null, delegate
                {
                    RestoreFromTray(false);
                    OpenSettingsDialog();
                });
                itemSettings.ForeColor = WinColors.TextWhite;

                ToolStripSeparator sep = new ToolStripSeparator();

                ToolStripMenuItem itemExit = new ToolStripMenuItem("Exit", null, delegate
                {
                    if (trayIcon != null) trayIcon.Visible = false;
                    Application.Exit();
                });
                itemExit.ForeColor = WinColors.WeakText;

                trayMenu.Items.Add(itemOpen);
                trayMenu.Items.Add(itemSearch);
                trayMenu.Items.Add(itemLock);
                trayMenu.Items.Add(itemSettings);
                trayMenu.Items.Add(sep);
                trayMenu.Items.Add(itemExit);

                trayIcon = new NotifyIcon();
                trayIcon.Text = "KeyCraft Password Manager";
                trayIcon.ContextMenuStrip = trayMenu;

                try
                {
                    Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    trayIcon.Icon = appIcon != null ? appIcon : SystemIcons.Shield;
                }
                catch
                {
                    trayIcon.Icon = SystemIcons.Shield;
                }

                trayIcon.DoubleClick += delegate { RestoreFromTray(false); };
                trayIcon.Click += delegate (object s, EventArgs ea)
                {
                    MouseEventArgs me = ea as MouseEventArgs;
                    if (me == null || me.Button == MouseButtons.Left)
                    {
                        RestoreFromTray(false);
                    }
                };

                notifyIcon = trayIcon;
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SingleInstanceController.UnregisterActiveForm(this);
            if (hotkeyManager != null)
            {
                hotkeyManager.Dispose();
                hotkeyManager = null;
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
            if (clipboardClearTimer != null)
            {
                clipboardClearTimer.Stop();
                clipboardClearTimer.Dispose();
                clipboardClearTimer = null;
            }
            if (resizeFilter != null)
            {
                Application.RemoveMessageFilter(resizeFilter);
                resizeFilter = null;
            }
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && AppSettings.Load().CloseToTray)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }

            try
            {
                AppSettings settings = AppSettings.Load();
                if (cboFilter != null && cboFilter.SelectedItem != null)
                {
                    settings.LastFilterBy = cboFilter.SelectedItem.ToString();
                }

                if (this.WindowState == FormWindowState.Normal)
                {
                    settings.WindowX = this.Location.X;
                    settings.WindowY = this.Location.Y;
                    settings.WindowWidth = this.Size.Width;
                    settings.WindowHeight = this.Size.Height;
                    settings.WindowMaximized = false;
                }
                else if (this.WindowState == FormWindowState.Maximized)
                {
                    settings.WindowMaximized = true;
                    if (this.RestoreBounds.Width > 0 && this.RestoreBounds.Height > 0)
                    {
                        settings.WindowX = this.RestoreBounds.X;
                        settings.WindowY = this.RestoreBounds.Y;
                        settings.WindowWidth = this.RestoreBounds.Width;
                        settings.WindowHeight = this.RestoreBounds.Height;
                    }
                }
                settings.Save();
            }
            catch { }
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= Win32Helper.CS_DROPSHADOW;
                cp.Style |= Win32Helper.WS_MINIMIZEBOX | Win32Helper.WS_MAXIMIZEBOX;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win32Helper.ApplyWindowShadow(this.Handle);
            ApplyHotkeyRegistration();
        }

        public void ApplyHotkeyRegistration()
        {
            if (hotkeyManager == null) return;
            hotkeyManager.Unregister();
            AppSettings s = AppSettings.Load();
            if (s != null && s.HotkeyEnabled && this.IsHandleCreated)
            {
                string err;
                hotkeyManager.Register(this.Handle, s.HotkeyModifiers, s.HotkeyKey, out err);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Win32Helper.ApplyWindowShadow(this.Handle);
            try
            {
                AppSettings settings = AppSettings.Load();
                if (settings.WindowWidth >= 800 && settings.WindowHeight >= 500)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(settings.WindowX, settings.WindowY);
                    this.Size = new Size(settings.WindowWidth, settings.WindowHeight);

                    Rectangle screenArea = Screen.FromControl(this).WorkingArea;
                    if (!screenArea.IntersectsWith(this.Bounds))
                    {
                        this.CenterToScreen();
                    }

                    if (settings.WindowMaximized)
                    {
                        this.WindowState = FormWindowState.Maximized;
                        if (btnMax != null)
                        {
                            btnMax.IsMaximized = true;
                            btnMax.Invalidate();
                        }
                    }
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "KeyCraft — [vault.kcrypt]";
            this.Size = new Size(1280, 820);
            this.MinimumSize = new Size(960, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = WinColors.Desktop;
            this.ForeColor = WinColors.TextWhite;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            // Register native window edge and corner resizing filter
            resizeFilter = new WindowResizeFilter(this, 6);
            Application.AddMessageFilter(resizeFilter);

            // Form Border Painting (only when not maximized)
            this.Paint += delegate (object s, PaintEventArgs pe)
            {
                if (this.WindowState != FormWindowState.Maximized)
                {
                    using (Pen p = new Pen(WinColors.Border, 1f))
                    {
                        pe.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                    }
                }
            };
            this.Resize += delegate { this.Invalidate(); };

            // =========================================================================
            // 1. TOP: NATIVE WINDOWS 11 TITLE BAR (Height 32)
            // =========================================================================
            panelTitleBar = new Panel();
            panelTitleBar.Dock = DockStyle.Top;
            panelTitleBar.Height = 32;
            panelTitleBar.BackColor = WinColors.Chrome;
            panelTitleBar.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            panelTitleBar.DoubleClick += (s, e) => ToggleMaximize();

            // App Identity Icon (Blue Shield)
            Panel pnlAppIcon = new Panel();
            pnlAppIcon.Location = new Point(12, 8);
            pnlAppIcon.Size = new Size(16, 16);
            pnlAppIcon.BackColor = Color.Transparent;
            pnlAppIcon.Paint += delegate (object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon("shield");
                if (bmp != null) IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), WinColors.Accent);
            };
            pnlAppIcon.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            panelTitleBar.Controls.Add(pnlAppIcon);

            // Title Text
            lblAppTitle = new Label();
            lblAppTitle.Text = "KeyCraft — [vault.kcrypt]";
            lblAppTitle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblAppTitle.ForeColor = WinColors.TextSecondary;
            lblAppTitle.Location = new Point(34, 7);
            lblAppTitle.AutoSize = true;
            lblAppTitle.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            panelTitleBar.Controls.Add(lblAppTitle);

            lblVaultBadge = new Label();
            lblVaultBadge.Visible = false;
            lblVaultBadge.Text = (service != null && !string.IsNullOrEmpty(service.StorageFilePath)) ? System.IO.Path.GetFileName(service.StorageFilePath) : "vault.kcrypt";
            panelTitleBar.Controls.Add(lblVaultBadge);

            // Caption Controls [Min] [Max] [Close]
            Panel pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Right;
            pnlButtons.Size = new Size(138, 32);

            btnMin = new TitleBarButton(TitleButtonType.Minimize);
            btnMin.Location = new Point(0, 0);
            btnMin.Size = new Size(46, 32);
            btnMin.Click += delegate { MinimizeToTray(); };
            pnlButtons.Controls.Add(btnMin);

            btnMax = new TitleBarButton(TitleButtonType.Maximize);
            btnMax.Location = new Point(46, 0);
            btnMax.Size = new Size(46, 32);
            btnMax.Click += delegate { ToggleMaximize(); };
            pnlButtons.Controls.Add(btnMax);

            btnClose = new TitleBarButton(TitleButtonType.Close);
            btnClose.Location = new Point(92, 0);
            btnClose.Size = new Size(46, 32);
            btnClose.Click += delegate { this.Close(); };
            pnlButtons.Controls.Add(btnClose);

            panelTitleBar.Controls.Add(pnlButtons);

            // TitleBar Bottom Divider
            Panel pnlTitleDiv = new Panel();
            pnlTitleDiv.Dock = DockStyle.Bottom;
            pnlTitleDiv.Height = 1;
            pnlTitleDiv.BackColor = WinColors.BorderSubtle;
            panelTitleBar.Controls.Add(pnlTitleDiv);

            // =========================================================================
            // 2. COMMANDBAR / TOOLBAR (Height 42)
            // Modern VS Code / Antigravity Menu-Bar Aesthetic
            // =========================================================================
            panelCommandBar = new Panel();
            panelCommandBar.Dock = DockStyle.Top;
            panelCommandBar.Height = 42;
            panelCommandBar.BackColor = WinColors.Chrome;
            panelCommandBar.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, panelCommandBar.Height - 1, panelCommandBar.Width, panelCommandBar.Height - 1);
                }
            };

            // + New Vault Button
            btnNewVault = new ModernButton();
            btnNewVault.Text = "New Vault";
            btnNewVault.IconName = "plus";
            btnNewVault.IconSize = 13;
            btnNewVault.Location = new Point(10, 8);
            btnNewVault.Size = new Size(96, 26);
            btnNewVault.CornerRadius = 4f;
            btnNewVault.NormalBg = Color.Transparent;
            btnNewVault.HoverBg = Color.FromArgb(48, 48, 48);
            btnNewVault.PressedBg = Color.FromArgb(64, 64, 64);
            btnNewVault.BorderColor = Color.Transparent;
            btnNewVault.NormalFg = WinColors.TextSecondary;
            btnNewVault.HoverFg = Color.White;
            btnNewVault.ShowFocusBorder = false;
            btnNewVault.Click += delegate { PromptCreateNewVault(); };
            panelCommandBar.Controls.Add(btnNewVault);

            // Open Button
            btnOpenVault = new ModernButton();
            btnOpenVault.Text = "Open";
            btnOpenVault.IconName = "folder";
            btnOpenVault.IconSize = 13;
            btnOpenVault.Location = new Point(110, 8);
            btnOpenVault.Size = new Size(72, 26);
            btnOpenVault.CornerRadius = 4f;
            btnOpenVault.NormalBg = Color.Transparent;
            btnOpenVault.HoverBg = Color.FromArgb(48, 48, 48);
            btnOpenVault.PressedBg = Color.FromArgb(64, 64, 64);
            btnOpenVault.BorderColor = Color.Transparent;
            btnOpenVault.NormalFg = WinColors.TextSecondary;
            btnOpenVault.HoverFg = Color.White;
            btnOpenVault.ShowFocusBorder = false;
            btnOpenVault.Click += delegate { PromptOpenExistingVault(); };
            panelCommandBar.Controls.Add(btnOpenVault);

            // Lock Button
            btnLockVault = new ModernButton();
            btnLockVault.Text = "Lock";
            btnLockVault.IconName = "lock";
            btnLockVault.IconSize = 13;
            btnLockVault.Location = new Point(186, 8);
            btnLockVault.Size = new Size(68, 26);
            btnLockVault.CornerRadius = 4f;
            btnLockVault.NormalBg = Color.Transparent;
            btnLockVault.HoverBg = Color.FromArgb(48, 48, 48);
            btnLockVault.PressedBg = Color.FromArgb(64, 64, 64);
            btnLockVault.BorderColor = Color.Transparent;
            btnLockVault.NormalFg = WinColors.TextSecondary;
            btnLockVault.HoverFg = Color.White;
            btnLockVault.ShowFocusBorder = false;
            btnLockVault.Click += delegate { LockAndShowUnlockScreen(); };
            panelCommandBar.Controls.Add(btnLockVault);

            // Refresh Button
            btnRefresh = new ModernButton();
            btnRefresh.Text = "Refresh";
            btnRefresh.IconName = "refresh";
            btnRefresh.IconSize = 13;
            btnRefresh.Location = new Point(258, 8);
            btnRefresh.Size = new Size(80, 26);
            btnRefresh.CornerRadius = 4f;
            btnRefresh.NormalBg = Color.Transparent;
            btnRefresh.HoverBg = Color.FromArgb(48, 48, 48);
            btnRefresh.PressedBg = Color.FromArgb(64, 64, 64);
            btnRefresh.BorderColor = Color.Transparent;
            btnRefresh.NormalFg = WinColors.TextSecondary;
            btnRefresh.HoverFg = Color.White;
            btnRefresh.ShowFocusBorder = false;
            btnRefresh.Click += delegate { LoadCredentials(); };
            panelCommandBar.Controls.Add(btnRefresh);

            // Settings Button with authentic Settings Gear icon
            btnSettings = new ModernButton();
            btnSettings.Text = "Settings";
            btnSettings.IconName = "settings";
            btnSettings.IconSize = 14;
            btnSettings.Location = new Point(342, 8);
            btnSettings.Size = new Size(86, 26);
            btnSettings.CornerRadius = 4f;
            btnSettings.NormalBg = Color.Transparent;
            btnSettings.HoverBg = Color.FromArgb(48, 48, 48);
            btnSettings.PressedBg = Color.FromArgb(64, 64, 64);
            btnSettings.BorderColor = Color.Transparent;
            btnSettings.NormalFg = WinColors.TextSecondary;
            btnSettings.HoverFg = Color.White;
            btnSettings.ShowFocusBorder = false;
            btnSettings.Click += delegate { OpenSettingsDialog(); };
            panelCommandBar.Controls.Add(btnSettings);

            // =========================================================================
            // 3. BOTTOM: SLEEK STATUS BAR (Height 24)
            // =========================================================================
            panelStatusBar = new Panel();
            panelStatusBar.Dock = DockStyle.Bottom;
            panelStatusBar.Height = 24;
            panelStatusBar.BackColor = WinColors.StatusBar;
            panelStatusBar.Padding = new Padding(12, 4, 12, 4);

            Panel pnlStatusDiv = new Panel();
            pnlStatusDiv.Dock = DockStyle.Top;
            pnlStatusDiv.Height = 1;
            pnlStatusDiv.BackColor = WinColors.BorderSubtle;
            panelStatusBar.Controls.Add(pnlStatusDiv);

            // Ready status dot
            Panel pnlReadyDot = new Panel();
            pnlReadyDot.Location = new Point(12, 9);
            pnlReadyDot.Size = new Size(6, 6);
            pnlReadyDot.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (SolidBrush b = new SolidBrush(WinColors.Success))
                {
                    pe.Graphics.FillEllipse(b, 0, 0, 6, 6);
                }
            };
            panelStatusBar.Controls.Add(pnlReadyDot);

            Label lblReady = new Label();
            lblReady.Text = "Ready";
            lblReady.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblReady.ForeColor = WinColors.TextMuted;
            lblReady.Location = new Point(22, 5);
            lblReady.AutoSize = true;
            panelStatusBar.Controls.Add(lblReady);

            Label lblSep1 = new Label { Text = "|", ForeColor = Color.FromArgb(64, 64, 64), Location = new Point(62, 5), AutoSize = true, Font = new Font("Segoe UI", 7.5f) };
            panelStatusBar.Controls.Add(lblSep1);

            lblStatusRecords = new Label();
            lblStatusRecords.Text = "Records: 14";
            lblStatusRecords.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblStatusRecords.ForeColor = WinColors.TextMuted;
            lblStatusRecords.Location = new Point(74, 5);
            lblStatusRecords.AutoSize = true;
            panelStatusBar.Controls.Add(lblStatusRecords);

            Label lblSep2 = new Label { Text = "|", ForeColor = Color.FromArgb(64, 64, 64), Location = new Point(144, 5), AutoSize = true, Font = new Font("Segoe UI", 7.5f) };
            panelStatusBar.Controls.Add(lblSep2);

            Label lblAutoLock = new Label();
            lblAutoLock.Text = "Auto-lock: 15m";
            lblAutoLock.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblAutoLock.ForeColor = WinColors.TextMuted;
            lblAutoLock.Location = new Point(156, 5);
            lblAutoLock.AutoSize = true;
            panelStatusBar.Controls.Add(lblAutoLock);

            // Right side format info
            Label lblFormat = new Label();
            lblFormat.Text = "Format: KCRYPT-v4 (Argon2id)";
            lblFormat.Font = new Font("Consolas", 7.5f, FontStyle.Regular);
            lblFormat.ForeColor = WinColors.TextSubtle;
            lblFormat.Dock = DockStyle.Right;
            lblFormat.TextAlign = ContentAlignment.MiddleRight;
            panelStatusBar.Controls.Add(lblFormat);

            // =========================================================================
            // 4. MAIN WORKSPACE (TWO-COLUMN SPLIT VIEW)
            // =========================================================================
            panelWorkspace = new Panel();
            panelWorkspace.Dock = DockStyle.Fill;
            panelWorkspace.BackColor = WinColors.Window;

            // -------------------------------------------------------------------------
            // RIGHT COLUMN: INSPECTOR / DETAIL PANE (Width 410)
            // -------------------------------------------------------------------------
            panelRight = new Panel();
            panelRight.Dock = DockStyle.Right;
            panelRight.Width = 410;
            panelRight.BackColor = WinColors.Card;
            panelRight.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, 0, panelRight.Height);
                }
            };

            // Form Actions Footer (p 12, bg #242424)
            Panel pnlRightFooter = new Panel();
            pnlRightFooter.Dock = DockStyle.Bottom;
            pnlRightFooter.Height = 94;
            pnlRightFooter.BackColor = WinColors.TableBg;
            pnlRightFooter.Padding = new Padding(16, 12, 16, 12);
            pnlRightFooter.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, pnlRightFooter.Width, 0);
                }
            };

            btnSave = new ModernButton();
            btnSave.Text = "Update Credential";
            btnSave.IconName = "save";
            btnSave.IconSize = 13;
            btnSave.NormalBg = WinColors.Accent;
            btnSave.HoverBg = WinColors.AccentHover;
            btnSave.PressedBg = WinColors.AccentPressed;
            btnSave.BorderColor = WinColors.Accent;
            btnSave.NormalFg = Color.White;
            btnSave.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnSave.Location = new Point(16, 10);
            btnSave.Size = new Size(378, 34);
            btnSave.Click += (s, e) => SaveOrUpdateCredential();
            pnlRightFooter.Controls.Add(btnSave);

            btnClearForm = new ModernButton();
            btnClearForm.Text = "New / Clear Form";
            btnClearForm.IconName = "plus";
            btnClearForm.IconSize = 13;
            btnClearForm.Location = new Point(16, 50);
            btnClearForm.Size = new Size(378, 32);
            btnClearForm.Click += delegate
            {
                if (!EnsureVaultLoadedForEditing()) return;
                RevertToNewCredential();
            };
            pnlRightFooter.Controls.Add(btnClearForm);

            // Right Body Container (Scrollable or Padded)
            Panel pnlRightBody = new Panel();
            pnlRightBody.Dock = DockStyle.Fill;
            pnlRightBody.BackColor = WinColors.Card;
            pnlRightBody.Padding = new Padding(16, 14, 16, 14);

            // Header Title
            lblEditorTitle = new Label();
            lblEditorTitle.Text = "Edit Credential";
            lblEditorTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblEditorTitle.ForeColor = WinColors.TextWhite;
            lblEditorTitle.Location = new Point(16, 12);
            lblEditorTitle.AutoSize = true;
            pnlRightBody.Controls.Add(lblEditorTitle);

            Panel pnlHeaderLine = new Panel();
            pnlHeaderLine.Location = new Point(16, 38);
            pnlHeaderLine.Size = new Size(378, 1);
            pnlHeaderLine.BackColor = WinColors.Border;
            pnlRightBody.Controls.Add(pnlHeaderLine);

            int rightY = 52;

            // SL NO Stepper
            Label lblSlTag = CreateInspectorLabel("SL NO", 16, rightY);
            pnlRightBody.Controls.Add(lblSlTag);

            txtSlNo = new TextBox();
            txtSlNo.Location = new Point(16, rightY + 20);
            txtSlNo.Size = new Size(64, 26);
            txtSlNo.BackColor = WinColors.InputBg;
            txtSlNo.ForeColor = WinColors.TextWhite;
            txtSlNo.BorderStyle = BorderStyle.FixedSingle;
            txtSlNo.TextAlign = HorizontalAlignment.Center;
            txtSlNo.Font = new Font("Consolas", 9.5f, FontStyle.Bold);
            txtSlNo.Text = "—";
            pnlRightBody.Controls.Add(txtSlNo);
            AttachVaultGuard(txtSlNo);

            btnStepUp = new ModernButton();
            btnStepUp.IconName = "chevron_up";
            btnStepUp.IconSize = 11;
            btnStepUp.Size = new Size(26, 26);
            btnStepUp.Location = new Point(84, rightY + 20);
            btnStepUp.Click += delegate
            {
                if (selectedCredential != null && lvCredentials != null && lvCredentials.SelectedIndices.Count > 0)
                {
                    MoveSelectedItem(-1);
                }
                else
                {
                    StepSelection(-1);
                }
            };
            pnlRightBody.Controls.Add(btnStepUp);

            btnStepDown = new ModernButton();
            btnStepDown.IconName = "chevron_down";
            btnStepDown.IconSize = 11;
            btnStepDown.Size = new Size(26, 26);
            btnStepDown.Location = new Point(114, rightY + 20);
            btnStepDown.Click += delegate
            {
                if (selectedCredential != null && lvCredentials != null && lvCredentials.SelectedIndices.Count > 0)
                {
                    MoveSelectedItem(1);
                }
                else
                {
                    StepSelection(1);
                }
            };
            pnlRightBody.Controls.Add(btnStepDown);

            lblSlNoTotal = new Label();
            lblSlNoTotal.Text = "of 0";
            lblSlNoTotal.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            lblSlNoTotal.ForeColor = WinColors.TextSubtle;
            lblSlNoTotal.Location = new Point(148, rightY + 25);
            lblSlNoTotal.AutoSize = true;
            pnlRightBody.Controls.Add(lblSlNoTotal);

            rightY += 58;

            // SERVICE / WEBSITE *
            Label lblServiceTag = CreateInspectorLabel("SERVICE / WEBSITE *", 16, rightY);
            pnlRightBody.Controls.Add(lblServiceTag);

            txtService = CreateFieldInput(pnlRightBody, 16, rightY + 20, 378);
            AttachVaultGuard(txtService);
            rightY += 58;

            // USERNAME / EMAIL (with embedded Copy Username button)
            Label lblUserTag = CreateInspectorLabel("USERNAME / EMAIL", 16, rightY);
            pnlRightBody.Controls.Add(lblUserTag);

            txtUsername = CreateFieldInput(pnlRightBody, 16, rightY + 20, 344);
            AttachVaultGuard(txtUsername);

            btnCopyUserField = new ModernButton();
            btnCopyUserField.IconName = "copy";
            btnCopyUserField.IconSize = 13;
            btnCopyUserField.Size = new Size(30, 26);
            btnCopyUserField.Location = new Point(364, rightY + 20);
            btnCopyUserField.Click += delegate
            {
                if (!string.IsNullOrEmpty(txtUsername.Text))
                {
                    Clipboard.SetText(txtUsername.Text);
                }
            };
            pnlRightBody.Controls.Add(btnCopyUserField);
            rightY += 58;

            // PASSWORD *
            Label lblPwdTag = CreateInspectorLabel("PASSWORD *", 16, rightY);
            pnlRightBody.Controls.Add(lblPwdTag);

            lblStrength = new Label();
            lblStrength.Text = "Strength: Weak";
            lblStrength.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblStrength.ForeColor = WinColors.WeakText;
            lblStrength.Location = new Point(270, rightY);
            lblStrength.Size = new Size(124, 16);
            lblStrength.TextAlign = ContentAlignment.MiddleRight;
            pnlRightBody.Controls.Add(lblStrength);

            // Password container with Eye & Gen buttons
            txtPassword = new TextBox();
            txtPassword.Location = new Point(16, rightY + 20);
            txtPassword.Size = new Size(280, 26);
            txtPassword.BackColor = WinColors.InputBg;
            txtPassword.ForeColor = WinColors.TextWhite;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.Font = new Font("Consolas", 10f, FontStyle.Regular);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += new EventHandler(TxtPassword_TextChanged);
            pnlRightBody.Controls.Add(txtPassword);
            AttachVaultGuard(txtPassword);

            btnTogglePassword = new ModernButton();
            btnTogglePassword.IconName = "eye";
            btnTogglePassword.IconSize = 13;
            btnTogglePassword.Size = new Size(30, 26);
            btnTogglePassword.Location = new Point(302, rightY + 20);
            btnTogglePassword.Click += delegate
            {
                isPasswordRevealed = !isPasswordRevealed;
                txtPassword.UseSystemPasswordChar = !isPasswordRevealed;
                btnTogglePassword.IconName = isPasswordRevealed ? "eye_off" : "eye";
                btnTogglePassword.Invalidate();
            };
            pnlRightBody.Controls.Add(btnTogglePassword);

            btnGeneratePassword = new ModernButton();
            btnGeneratePassword.Text = "Gen";
            btnGeneratePassword.IconName = "key";
            btnGeneratePassword.IconSize = 12;
            btnGeneratePassword.Size = new Size(58, 26);
            btnGeneratePassword.Location = new Point(336, rightY + 20);
            btnGeneratePassword.Click += delegate
            {
                if (!EnsureVaultLoadedForEditing()) return;
                string generated = service.GeneratePassword(16);
                txtPassword.Text = generated;
                txtPassword.UseSystemPasswordChar = false;
                isPasswordRevealed = true;
                btnTogglePassword.IconName = "eye_off";
                btnTogglePassword.Invalidate();
            };
            pnlRightBody.Controls.Add(btnGeneratePassword);
            rightY += 58;

            // SECURITY NOTES / URLS
            Label lblNotesTag = CreateInspectorLabel("SECURITY NOTES / URLS", 16, rightY);
            pnlRightBody.Controls.Add(lblNotesTag);

            txtNotes = new TextBox();
            txtNotes.Multiline = true;
            txtNotes.Location = new Point(16, rightY + 20);
            txtNotes.Size = new Size(378, 68);
            txtNotes.BackColor = WinColors.InputBg;
            txtNotes.ForeColor = WinColors.TextWhite;
            txtNotes.BorderStyle = BorderStyle.FixedSingle;
            txtNotes.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            pnlRightBody.Controls.Add(txtNotes);
            AttachVaultGuard(txtNotes);
            rightY += 96;

            // Metadata footer line
            Panel pnlMetaLine = new Panel();
            pnlMetaLine.Location = new Point(16, rightY);
            pnlMetaLine.Size = new Size(378, 1);
            pnlMetaLine.BackColor = WinColors.Border;
            pnlRightBody.Controls.Add(pnlMetaLine);
            rightY += 10;

            lblLastModified = new Label();
            lblLastModified.Text = "Last Modified: —";
            lblLastModified.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblLastModified.ForeColor = WinColors.TextSubtle;
            lblLastModified.Location = new Point(16, rightY);
            lblLastModified.AutoSize = true;
            pnlRightBody.Controls.Add(lblLastModified);

            lblRecordId = new Label();
            lblRecordId.Text = "ID: —";
            lblRecordId.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblRecordId.ForeColor = WinColors.TextSubtle;
            lblRecordId.Location = new Point(340, rightY);
            lblRecordId.Size = new Size(54, 16);
            lblRecordId.TextAlign = ContentAlignment.MiddleRight;
            pnlRightBody.Controls.Add(lblRecordId);

            panelRight.Controls.Add(pnlRightBody);
            panelRight.Controls.Add(pnlRightFooter);

            // -------------------------------------------------------------------------
            // LEFT COLUMN: DATAGRID & MASTER LIST
            // -------------------------------------------------------------------------
            panelLeft = new Panel();
            panelLeft.Dock = DockStyle.Fill;
            panelLeft.BackColor = WinColors.Card;

            // Table Filter Bar (p 10, bg #242424)
            Panel pnlFilterBar = new Panel();
            pnlFilterBar.Dock = DockStyle.Top;
            pnlFilterBar.Height = 44;
            pnlFilterBar.BackColor = WinColors.TableBg;
            pnlFilterBar.Padding = new Padding(10, 8, 10, 8);
            pnlFilterBar.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, pnlFilterBar.Height - 1, pnlFilterBar.Width, pnlFilterBar.Height - 1);
                }
            };

            // Search Container: [Search icon] [Input] [X button]
            Panel pnlSearch = new Panel();
            pnlSearch.Location = new Point(10, 8);
            pnlSearch.Size = new Size(580, 28);
            pnlSearch.BackColor = Color.FromArgb(46, 46, 46);
            pnlSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlSearch.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(txtSearch.Focused ? WinColors.Accent : WinColors.BorderLight, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
                }
                Bitmap bmp = IconResources.GetIcon("search");
                if (bmp != null) IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(8, 7, 14, 14), WinColors.TextSubtle);
            };

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Color.FromArgb(46, 46, 46);
            txtSearch.ForeColor = WinColors.TextWhite;
            txtSearch.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            txtSearch.Location = new Point(28, 6);
            txtSearch.Size = new Size(520, 16);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            txtSearch.GotFocus += (s, e) => pnlSearch.Invalidate();
            txtSearch.LostFocus += (s, e) => pnlSearch.Invalidate();
            pnlSearch.Controls.Add(txtSearch);

            btnClearSearch = new ModernButton();
            btnClearSearch.IconName = "xmark";
            btnClearSearch.IconSize = 10;
            btnClearSearch.Size = new Size(20, 20);
            btnClearSearch.Location = new Point(pnlSearch.Width - 24, 4);
            btnClearSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClearSearch.NormalBg = Color.Transparent;
            btnClearSearch.HoverBg = Color.FromArgb(64, 64, 64);
            btnClearSearch.BorderColor = Color.Transparent;
            btnClearSearch.Click += delegate { txtSearch.Text = string.Empty; };
            pnlSearch.Controls.Add(btnClearSearch);

            pnlFilterBar.Controls.Add(pnlSearch);

            // Filter Dropdown (Windows 11 Fluent ModernComboBox)
            cboFilter = new ModernComboBox();
            cboFilter.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            cboFilter.Items.AddRange(new object[] { "Filter by: SlNo", "Filter by: All", "Filter by: Service", "Filter by: Username", "Filter by: Weak Passwords" });

            AppSettings initialSettings = AppSettings.Load();
            int defaultFilterIndex = 0; // Default: Filter by: SlNo
            if (!string.IsNullOrEmpty(initialSettings.LastFilterBy))
            {
                string savedFilter = initialSettings.LastFilterBy.Trim();
                for (int i = 0; i < cboFilter.Items.Count; i++)
                {
                    string it = cboFilter.Items[i].ToString();
                    if (string.Equals(it, savedFilter, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(it.Replace("Filter by: ", "").Trim(), savedFilter.Replace("Filter by: ", "").Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        defaultFilterIndex = i;
                        break;
                    }
                }
            }
            cboFilter.SelectedIndex = defaultFilterIndex;
            cboFilter.Location = new Point(600, 8);
            cboFilter.Size = new Size(185, 28);
            cboFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cboFilter.SelectedIndexChanged += delegate
            {
                if (cboFilter.SelectedItem != null)
                {
                    try
                    {
                        AppSettings s = AppSettings.Load();
                        s.LastFilterBy = cboFilter.SelectedItem.ToString();
                        s.Save();
                    }
                    catch { }
                }

                if (cmbSearchColumn != null)
                {
                    string txt = cboFilter.Text ?? "";
                    if (txt.IndexOf("SlNo", StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("Sl No", StringComparison.OrdinalIgnoreCase) >= 0)
                        cmbSearchColumn.SelectedItem = "Sl No";
                    else if (txt.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0)
                        cmbSearchColumn.SelectedItem = "Service";
                    else if (txt.IndexOf("Username", StringComparison.OrdinalIgnoreCase) >= 0)
                        cmbSearchColumn.SelectedItem = "Username";
                    else if (txt.IndexOf("All", StringComparison.OrdinalIgnoreCase) >= 0)
                        cmbSearchColumn.SelectedItem = "All";
                }
                ApplyFilter();
            };
            pnlFilterBar.Controls.Add(cboFilter);

            pnlFilterBar.Resize += delegate
            {
                cboFilter.Location = new Point(pnlFilterBar.ClientSize.Width - 195, 8);
                pnlSearch.Width = Math.Max(100, pnlFilterBar.ClientSize.Width - 215);
            };

            // Table Action Footer (Copy Password, Copy Username, Delete, Total count)
            Panel pnlTableFooter = new Panel();
            pnlTableFooter.Dock = DockStyle.Bottom;
            pnlTableFooter.Height = 44;
            pnlTableFooter.BackColor = WinColors.TableBg;
            pnlTableFooter.Padding = new Padding(12, 8, 12, 8);
            pnlTableFooter.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawLine(p, 0, 0, pnlTableFooter.Width, 0);
                }
            };

            btnCopyPassword = new ModernButton();
            btnCopyPassword.Text = "Copy Password";
            btnCopyPassword.IconName = "copy";
            btnCopyPassword.IconSize = 13;
            btnCopyPassword.Size = new Size(136, 28);
            btnCopyPassword.Location = new Point(12, 8);
            btnCopyPassword.Click += (s, e) => CopySelectedPassword();
            pnlTableFooter.Controls.Add(btnCopyPassword);

            btnCopyUsername = new ModernButton();
            btnCopyUsername.Text = "Copy Username";
            btnCopyUsername.IconName = "user";
            btnCopyUsername.IconSize = 13;
            btnCopyUsername.Size = new Size(136, 28);
            btnCopyUsername.Location = new Point(156, 8);
            btnCopyUsername.Click += (s, e) => CopySelectedUsername();
            pnlTableFooter.Controls.Add(btnCopyUsername);

            btnDelete = new ModernButton();
            btnDelete.Text = "Delete";
            btnDelete.IconName = "trash";
            btnDelete.IconSize = 13;
            btnDelete.NormalBg = WinColors.BtnSecondary;
            btnDelete.HoverBg = Color.FromArgb(61, 39, 39);
            btnDelete.BorderColor = WinColors.DangerBorder;
            btnDelete.NormalFg = WinColors.DangerText;
            btnDelete.Size = new Size(86, 28);
            btnDelete.Location = new Point(300, 8);
            btnDelete.Click += (s, e) => DeleteSelectedCredential();
            pnlTableFooter.Controls.Add(btnDelete);

            lblTotalCount = new Label();
            lblTotalCount.Text = "Total: 0";
            lblTotalCount.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblTotalCount.ForeColor = WinColors.TextMuted;
            lblTotalCount.Dock = DockStyle.Right;
            lblTotalCount.TextAlign = ContentAlignment.MiddleRight;
            lblTotalCount.Padding = new Padding(0, 4, 12, 0);
            pnlTableFooter.Controls.Add(lblTotalCount);

            // DataGrid / ListView Table
            lvCredentials = new FluentListView();
            lvCredentials.Dock = DockStyle.Fill;
            lvCredentials.View = View.Details;
            lvCredentials.FullRowSelect = true;
            lvCredentials.MultiSelect = false;
            lvCredentials.BackColor = WinColors.TableBg;
            lvCredentials.ForeColor = WinColors.TextWhite;
            lvCredentials.BorderStyle = BorderStyle.None;
            lvCredentials.Font = new Font("Segoe UI", 9f);
            lvCredentials.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lvCredentials.HandleCreated += delegate
            {
                NativeMethods.SetWindowTheme(lvCredentials.Handle, "DarkMode_Explorer", null);
                FluentListView flv = lvCredentials as FluentListView;
                if (flv != null)
                {
                    flv.EnableDoubleBuffer();
                    flv.AttachHeaderSubclass();
                }
            };
            if (lvCredentials.IsHandleCreated)
            {
                NativeMethods.SetWindowTheme(lvCredentials.Handle, "DarkMode_Explorer", null);
                FluentListView flv = lvCredentials as FluentListView;
                if (flv != null)
                {
                    flv.EnableDoubleBuffer();
                    flv.AttachHeaderSubclass();
                }
            }

            lvCredentials.Columns.Add("SL No", 54, HorizontalAlignment.Center);
            lvCredentials.Columns.Add("Service / Website", 190);
            lvCredentials.Columns.Add("Username / Email", 200);
            lvCredentials.Columns.Add("Password", 160);
            lvCredentials.Columns.Add("Last Updated", 120, HorizontalAlignment.Right);

            // Owner-drawn Table Headers matching Stitch Screen 2
            lvCredentials.OwnerDraw = true;
            lvCredentials.DrawColumnHeader += delegate (object s, DrawListViewColumnHeaderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(35, 35, 35)))
                {
                    e.Graphics.FillRectangle(b, e.Bounds);
                }
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    e.Graphics.DrawLine(p, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
                }

                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
                if (e.Header.TextAlign == HorizontalAlignment.Center) flags |= TextFormatFlags.HorizontalCenter;
                else if (e.Header.TextAlign == HorizontalAlignment.Right) flags |= TextFormatFlags.Right;
                else flags |= TextFormatFlags.Left;

                Rectangle r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
                using (Font hf = new Font("Segoe UI", 8f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(e.Graphics, e.Header.Text, hf, r, WinColors.TextMuted, flags);
                }
            };

            // Owner-drawn Rows matching Stitch Screen 2
            lvCredentials.DrawItem += delegate (object s, DrawListViewItemEventArgs e)
            {
                e.DrawDefault = false;
            };

            lvCredentials.DrawSubItem += delegate (object s, DrawListViewSubItemEventArgs e)
            {
                e.DrawDefault = false;
                bool isSelected = e.Item.Selected;
                FluentListView flv = lvCredentials as FluentListView;
                bool isHovered = (flv != null && flv.HoveredIndex == e.ItemIndex);
                Rectangle b = e.Bounds;

                // Row background with smooth Fluent dark hover highlight
                Color rowBg;
                if (isSelected)
                {
                    rowBg = isHovered ? Color.FromArgb(64, 64, 64) : Color.FromArgb(56, 56, 56);
                }
                else if (isHovered)
                {
                    rowBg = Color.FromArgb(46, 46, 46);
                }
                else
                {
                    rowBg = (e.ItemIndex % 2 == 0) ? WinColors.TableBg : Color.FromArgb(38, 38, 38);
                }

                using (SolidBrush bgBrush = new SolidBrush(rowBg))
                {
                    e.Graphics.FillRectangle(bgBrush, b);
                }

                // 3px Inset Blue Accent line for selected row
                if (isSelected && e.ColumnIndex == 0)
                {
                    using (SolidBrush selBar = new SolidBrush(WinColors.Accent))
                    {
                        e.Graphics.FillRectangle(selBar, b.Left, b.Top, 3, b.Height);
                    }
                }

                // Cell grid lines
                using (Pen cellPen = new Pen(WinColors.BorderSubtle, 1f))
                {
                    e.Graphics.DrawLine(cellPen, b.Right - 1, b.Top, b.Right - 1, b.Bottom);
                    e.Graphics.DrawLine(cellPen, b.Left, b.Bottom - 1, b.Right, b.Bottom - 1);
                }

                Credential cred = e.Item.Tag as Credential;
                if (cred == null) return;

                int col = e.ColumnIndex;
                if (col == 0)
                {
                    // SL No
                    using (Font mf = new Font("Consolas", 8.5f, FontStyle.Bold))
                    {
                        Color c = isSelected ? WinColors.Accent : (isHovered ? WinColors.TextSecondary : WinColors.TextSubtle);
                        TextRenderer.DrawText(e.Graphics, e.SubItem.Text, mf, b, c, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                    }
                }
                else if (col == 1)
                {
                    // Service with Globe Icon
                    Bitmap globeBmp = IconResources.GetIcon("globe");
                    int iconY = b.Top + (b.Height - 14) / 2;
                    if (globeBmp != null)
                    {
                        Color iconColor = isSelected ? WinColors.Accent : (isHovered ? WinColors.AccentHover : WinColors.TextSubtle);
                        IconHelper.DrawTintedIcon(e.Graphics, globeBmp, new Rectangle(b.Left + 8, iconY, 14, 14), iconColor);
                    }
                    Rectangle textRect = new Rectangle(b.Left + 28, b.Top, b.Width - 32, b.Height);
                    Color titleColor = (isSelected || isHovered) ? WinColors.TextWhite : Color.FromArgb(235, 235, 235);
                    TextRenderer.DrawText(e.Graphics, cred.Service, lvCredentials.Font, textRect, titleColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
                else if (col == 2)
                {
                    // Username (mono font)
                    Rectangle textRect = new Rectangle(b.Left + 8, b.Top, b.Width - 12, b.Height);
                    using (Font mf = new Font("Consolas", 8.5f, FontStyle.Regular))
                    {
                        Color userColor = isSelected ? WinColors.TextWhite : (isHovered ? WinColors.TextWhite : WinColors.TextSecondary);
                        TextRenderer.DrawText(e.Graphics, cred.Username, mf, textRect, userColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    }
                }
                else if (col == 3)
                {
                    // Password dots + Strength Pill
                    string strengthLabel;
                    int score = service.EvaluateStrength(cred.Password, out strengthLabel);

                    // Dots
                    using (Font mf = new Font("Consolas", 9f, FontStyle.Regular))
                    {
                        Color dotColor = (isSelected || isHovered) ? WinColors.TextSecondary : WinColors.TextSubtle;
                        TextRenderer.DrawText(e.Graphics, "••••••••", mf, new Rectangle(b.Left + 8, b.Top, 70, b.Height), dotColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    }

                    // Strength Badge Pill
                    Color badgeBg = WinColors.WeakBg;
                    Color badgeBorder = WinColors.WeakBorder;
                    Color badgeText = WinColors.WeakText;

                    if (score == 2)
                    {
                        badgeBg = WinColors.MediumBg;
                        badgeBorder = WinColors.MediumBorder;
                        badgeText = WinColors.MediumText;
                    }
                    else if (score >= 3)
                    {
                        badgeBg = WinColors.SuccessBg;
                        badgeBorder = WinColors.SuccessBorder;
                        badgeText = WinColors.SuccessLight;
                    }

                    Rectangle pillRect = new Rectangle(b.Right - 54, b.Top + (b.Height - 18) / 2, 46, 18);
                    using (SolidBrush pillBrush = new SolidBrush(badgeBg))
                    {
                        e.Graphics.FillRectangle(pillBrush, pillRect);
                    }
                    using (Pen pillPen = new Pen(badgeBorder, 1f))
                    {
                        e.Graphics.DrawRectangle(pillPen, pillRect);
                    }
                    using (Font pillFont = new Font("Segoe UI", 7f, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, strengthLabel, pillFont, pillRect, badgeText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }
                else if (col == 4)
                {
                    // Last Updated (Right aligned mono)
                    Rectangle textRect = new Rectangle(b.Left + 4, b.Top, b.Width - 12, b.Height);
                    using (Font mf = new Font("Consolas", 8f, FontStyle.Regular))
                    {
                        Color dateColor = (isSelected || isHovered) ? WinColors.TextSecondary : WinColors.TextSubtle;
                        TextRenderer.DrawText(e.Graphics, cred.LastUpdated.ToString("yyyy-MM-dd"), mf, textRect, dateColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                    }
                }
            };

            lvCredentials.SelectedIndexChanged += new EventHandler(LvCredentials_SelectedIndexChanged);
            lvCredentials.Resize += delegate
            {
                AdjustTableColumns();
                if (lvCredentials.IsHandleCreated)
                {
                    NativeMethods.ShowScrollBar(lvCredentials.Handle, NativeMethods.SB_HORZ, false);
                }
            };
            lvCredentials.ColumnWidthChanged += delegate
            {
                if (lvCredentials.IsHandleCreated)
                {
                    NativeMethods.ShowScrollBar(lvCredentials.Handle, NativeMethods.SB_HORZ, false);
                }
            };

            // Build empty state overlay card
            BuildTableEmptyOverlay();

            // Natural docking order (reverse of add order):
            panelLeft.Controls.Add(pnlTableOverlay); // Index 0: Fill (when active)
            panelLeft.Controls.Add(lvCredentials);   // Index 1: Fill
            panelLeft.Controls.Add(pnlTableFooter);  // Index 2: Bottom
            panelLeft.Controls.Add(pnlFilterBar);    // Index 3: Top

            panelWorkspace.Controls.Add(panelLeft);  // Index 0: Fill
            panelWorkspace.Controls.Add(panelRight); // Index 1: Right

            this.Controls.Add(panelWorkspace);   // Index 0: Fill
            this.Controls.Add(panelStatusBar);   // Index 1: Bottom
            this.Controls.Add(panelCommandBar);  // Index 2: Top
            this.Controls.Add(panelTitleBar);    // Index 3: Top (absolute top edge)

            this.Shown += delegate { AdjustTableColumns(); };

            txtSerialNo = txtSlNo;
            btnClear = btnClearForm;
            btnMoveUp = btnStepUp;
            btnMoveDown = btnStepDown;
            notifyIcon = trayIcon;

            lblToast = new Label();
            lblToast.Visible = false;
            this.Controls.Add(lblToast);

            cmbSearchColumn = new ComboBox();
            cmbSearchColumn.Visible = false;
            cmbSearchColumn.Items.AddRange(new object[] { "Sl No", "All", "Service", "Username", "Password" });
            cmbSearchColumn.SelectedIndex = 0;
            cmbSearchColumn.SelectedIndexChanged += delegate
            {
                if (cmbSearchColumn.SelectedItem != null && cboFilter != null)
                {
                    string col = cmbSearchColumn.SelectedItem.ToString();
                    if (string.Equals(col, "Sl No", StringComparison.OrdinalIgnoreCase) || string.Equals(col, "SlNo", StringComparison.OrdinalIgnoreCase))
                        cboFilter.SelectedItem = "Filter by: SlNo";
                    else if (string.Equals(col, "Service", StringComparison.OrdinalIgnoreCase))
                        cboFilter.SelectedItem = "Filter by: Service";
                    else if (string.Equals(col, "Username", StringComparison.OrdinalIgnoreCase))
                        cboFilter.SelectedItem = "Filter by: Username";
                    else if (string.Equals(col, "All", StringComparison.OrdinalIgnoreCase))
                        cboFilter.SelectedItem = "Filter by: All";
                }
                ApplyFilter();
            };
            this.Controls.Add(cmbSearchColumn);
        }

        private Panel CreateToolbarSeparator(int x)
        {
            Panel p = new Panel();
            p.Location = new Point(x, 13);
            p.Size = new Size(1, 16);
            p.BackColor = WinColors.BorderLight;
            return p;
        }

        private Label CreateInspectorLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lbl.ForeColor = WinColors.TextMuted;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private TextBox CreateFieldInput(Panel parent, int x, int y, int w)
        {
            TextBox tb = new TextBox();
            tb.Location = new Point(x, y);
            tb.Size = new Size(w, 26);
            tb.BackColor = WinColors.InputBg;
            tb.ForeColor = WinColors.TextWhite;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            parent.Controls.Add(tb);
            return tb;
        }

        private void AdjustTableColumns()
        {
            if (isAdjustingColumns || lvCredentials == null || lvCredentials.Columns.Count < 5) return;
            try
            {
                isAdjustingColumns = true;
                if (lvCredentials.IsHandleCreated)
                {
                    NativeMethods.SetWindowTheme(lvCredentials.Handle, "DarkMode_Explorer", null);
                    FluentListView flv = lvCredentials as FluentListView;
                    if (flv != null) flv.EnableDoubleBuffer();
                }
                int fixedWidth = lvCredentials.Columns[0].Width + lvCredentials.Columns[3].Width + lvCredentials.Columns[4].Width;
                // Safety margin of 6px guarantees columns never exceed client width, suppressing horizontal scrollbar
                int remaining = lvCredentials.ClientSize.Width - fixedWidth - 6;
                if (remaining > 200)
                {
                    int half = remaining / 2;
                    lvCredentials.Columns[1].Width = half;
                    lvCredentials.Columns[2].Width = Math.Max(50, remaining - half);
                }
                if (lvCredentials.IsHandleCreated)
                {
                    NativeMethods.ShowScrollBar(lvCredentials.Handle, NativeMethods.SB_HORZ, false);
                }
            }
            finally
            {
                isAdjustingColumns = false;
            }
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                btnMax.IsMaximized = false;
                btnMax.Invalidate();
            }
            else
            {
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
                btnMax.IsMaximized = true;
                btnMax.Invalidate();
            }
        }

        // ==========================================
        // DATA BINDING & EVENTS
        // ==========================================
        public void LoadCredentials()
        {
            try
            {
                if (service == null || !service.HasActiveVault)
                {
                    cachedList = new List<Credential>();
                    ApplyFilter();
                    UpdateVaultStatusUI();
                    return;
                }

                cachedList = service.LoadAll();
                ApplyFilter();
                UpdateVaultStatusUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load credentials: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            lvCredentials.Items.Clear();
            FluentListView flv = lvCredentials as FluentListView;
            if (flv != null) flv.ResetHover();

            string filterText = cboFilter != null ? (cboFilter.Text ?? "") : "";
            if (cmbSearchColumn != null && cmbSearchColumn.SelectedItem != null)
            {
                string sel = cmbSearchColumn.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(sel) && !string.Equals(sel, "All", StringComparison.OrdinalIgnoreCase) && !string.Equals(sel, "Filter by: All", StringComparison.OrdinalIgnoreCase))
                {
                    filterText = sel;
                }
                else if (string.Equals(sel, "All", StringComparison.OrdinalIgnoreCase))
                {
                    filterText = "All";
                }
            }

            FilterMode mode = FilterMode.All;
            if (filterText.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0) mode = FilterMode.Service;
            else if (filterText.IndexOf("Username", StringComparison.OrdinalIgnoreCase) >= 0) mode = FilterMode.Username;
            else if (filterText.IndexOf("Weak", StringComparison.OrdinalIgnoreCase) >= 0) mode = FilterMode.Weak;

            List<Credential> filtered;
            if (filterText.IndexOf("Sl No", StringComparison.OrdinalIgnoreCase) >= 0 || filterText.IndexOf("SlNo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filtered = service.Filter(cachedList, txtSearch.Text, "Sl No");
            }
            else
            {
                filtered = service.Filter(cachedList, txtSearch.Text, mode);
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                Credential c = filtered[i];
                string slNoStr = (c.SerialNo > 0) ? c.SerialNo.ToString() : (i + 1).ToString();
                ListViewItem item = new ListViewItem(slNoStr);
                item.SubItems.Add(c.Service);
                item.SubItems.Add(c.Username);
                item.SubItems.Add("••••••••");
                item.SubItems.Add(c.LastUpdated.ToString("yyyy-MM-dd"));
                item.Tag = c;
                lvCredentials.Items.Add(item);
            }

            lblTotalCount.Text = "Total: " + cachedList.Count;
            lblStatusRecords.Text = "Records: " + cachedList.Count;
            lblSlNoTotal.Text = "of " + cachedList.Count;

            if (lvCredentials.Items.Count > 0)
            {
                lvCredentials.Items[0].Selected = true;
            }
            else
            {
                RevertToNewCredential();
            }
        }

        private void LvCredentials_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selIdx = -1;
            if (lvCredentials.SelectedIndices.Count > 0)
            {
                selIdx = lvCredentials.SelectedIndices[0];
            }
            else if (lvCredentials.SelectedItems.Count > 0)
            {
                selIdx = lvCredentials.SelectedItems[0].Index;
            }
            else
            {
                for (int i = 0; i < lvCredentials.Items.Count; i++)
                {
                    if (lvCredentials.Items[i].Selected)
                    {
                        selIdx = i;
                        break;
                    }
                }
            }

            if (selIdx >= 0 && selIdx < lvCredentials.Items.Count)
            {
                currentSelectedIndex = selIdx;
                selectedCredential = lvCredentials.Items[selIdx].Tag as Credential;
                if (selectedCredential != null)
                {
                    lblEditorTitle.Text = "Edit Credential";
                    txtSlNo.Text = (currentSelectedIndex + 1).ToString();
                    txtService.Text = selectedCredential.Service;
                    txtUsername.Text = selectedCredential.Username;
                    txtPassword.Text = selectedCredential.Password;
                    txtNotes.Text = selectedCredential.Notes ?? "";
                    lblLastModified.Text = "Last Modified: " + selectedCredential.LastUpdated.ToString("yyyy-MM-dd HH:mm");
                    lblRecordId.Text = "ID: #" + (currentSelectedIndex + 1);
                    btnSave.Text = "Update Credential";

                    if (btnCopyPassword != null) btnCopyPassword.Enabled = true;
                    if (btnCopyUsername != null) btnCopyUsername.Enabled = true;
                    if (btnDelete != null) btnDelete.Enabled = true;
                }
            }
            else
            {
                RevertToNewCredential();
            }
        }

        private void StepSelection(int delta)
        {
            if (lvCredentials.Items.Count <= 1) return;
            int next = currentSelectedIndex + delta;
            if (next < 0) next = 0;
            if (next >= lvCredentials.Items.Count) next = lvCredentials.Items.Count - 1;

            lvCredentials.Items[next].Selected = true;
            lvCredentials.Items[next].EnsureVisible();
        }

        private void RevertToNewCredential()
        {
            selectedCredential = null;
            bool hasVault = service != null && service.HasActiveVault;
            lblEditorTitle.Text = hasVault ? "New Credential" : "No Vault Loaded";

            SetInspectorEnabled(hasVault);

            if (btnCopyPassword != null) btnCopyPassword.Enabled = false;
            if (btnCopyUsername != null) btnCopyUsername.Enabled = false;
            if (btnDelete != null) btnDelete.Enabled = false;
            if (btnStepUp != null) btnStepUp.Enabled = hasVault && lvCredentials != null && lvCredentials.Items.Count > 1;
            if (btnStepDown != null) btnStepDown.Enabled = hasVault && lvCredentials != null && lvCredentials.Items.Count > 1;
            if (!hasVault)
            {
                txtSlNo.Text = "—";
                lblSlNoTotal.Text = "of 0";
                lblLastModified.Text = "Last Modified: —";
                lblRecordId.Text = "ID: —";
            }
            else if (lvCredentials.Items.Count == 0)
            {
                txtSlNo.Text = "1";
                lblSlNoTotal.Text = "of 0";
                lblLastModified.Text = "Last Modified: —";
                lblRecordId.Text = "ID: (New)";
            }
            else
            {
                txtSlNo.Text = (lvCredentials.Items.Count + 1).ToString();
                lblSlNoTotal.Text = "of " + lvCredentials.Items.Count;
                lblLastModified.Text = "Last Modified: Just now";
                lblRecordId.Text = "ID: #" + (lvCredentials.Items.Count + 1);
            }
            txtService.Text = string.Empty;
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
            txtNotes.Text = string.Empty;
            btnSave.Text = "Save Credential";
            lblStrength.Text = "Strength: None";
            lblStrength.ForeColor = WinColors.TextMuted;
            isPasswordRevealed = false;
            txtPassword.UseSystemPasswordChar = true;
            btnTogglePassword.IconName = "eye";
            btnTogglePassword.Invalidate();
        }

        private void SaveOrUpdateCredential()
        {
            if (!EnsureVaultLoadedForEditing()) return;

            string sName = txtService.Text;
            string uName = txtUsername.Text;
            string pwd = txtPassword.Text;
            string notes = txtNotes.Text;

            string err;
            if (!service.Validate(sName, uName, pwd, out err))
            {
                MessageBox.Show(this, err, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (selectedCredential == null)
                {
                    service.AddCredential(sName, uName, pwd, notes);
                }
                else
                {
                    service.UpdateCredential(selectedCredential.Id, sName, uName, pwd, notes);
                    int targetSl;
                    if (int.TryParse(txtSlNo.Text.Trim(), out targetSl) && targetSl > 0)
                    {
                        service.ReorderCredential(selectedCredential.Id, targetSl);
                    }
                }

                LoadCredentials();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Save failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedCredential()
        {
            if (selectedCredential == null) return;
            DialogResult res = MessageBox.Show(
                this,
                string.Format("Are you sure you want to delete the credential for '{0}'?", selectedCredential.Service),
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (res == DialogResult.Yes)
            {
                service.DeleteCredential(selectedCredential.Id);
                LoadCredentials();
            }
        }

        private void CopySelectedPassword()
        {
            string pwd = null;
            if (selectedCredential != null && !string.IsNullOrEmpty(selectedCredential.Password))
            {
                pwd = selectedCredential.Password;
            }
            else if (lvCredentials != null && lvCredentials.Items.Count > 0)
            {
                for (int i = 0; i < lvCredentials.Items.Count; i++)
                {
                    if (lvCredentials.Items[i].Selected && lvCredentials.Items[i].Tag is Credential)
                    {
                        pwd = ((Credential)lvCredentials.Items[i].Tag).Password;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(txtPassword.Text))
            {
                pwd = txtPassword.Text;
            }

            if (!string.IsNullOrEmpty(pwd))
            {
                CopyToClipboardWithAutoClear(pwd);
                if (lblStatusRecords != null) lblStatusRecords.Text = "Password copied! (Clipboard auto-clears in 30s)";
                if (lblToast != null) lblToast.Text = "Password copied to clipboard!";
            }
        }

        private void CopySelectedUsername()
        {
            string uname = null;
            if (selectedCredential != null && !string.IsNullOrEmpty(selectedCredential.Username))
            {
                uname = selectedCredential.Username;
            }
            else if (!string.IsNullOrEmpty(txtUsername.Text))
            {
                uname = txtUsername.Text;
            }

            if (!string.IsNullOrEmpty(uname))
            {
                Clipboard.SetText(uname);
                if (lblStatusRecords != null) lblStatusRecords.Text = "Username copied to clipboard.";
            }
        }

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            string label;
            int score = service.EvaluateStrength(txtPassword.Text, out label);

            Color c = WinColors.TextMuted;
            if (score == 1) c = WinColors.WeakText;
            else if (score == 2) c = WinColors.MediumText;
            else if (score >= 3) c = WinColors.SuccessLight;

            lblStrength.Text = "Strength: " + label;
            lblStrength.ForeColor = c;
        }

        public void MinimizeToTray()
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                previousWindowState = this.WindowState;
            }
            this.Hide();
            this.ShowInTaskbar = false;
            if (trayIcon != null)
            {
                trayIcon.Visible = true;
                if (!hasShownTrayTip)
                {
                    try
                    {
                        trayIcon.ShowBalloonTip(
                            2000,
                            "KeyCraft Running in Background",
                            "KeyCraft is minimized to the system tray. Press the shortcut or double-click to reopen.",
                            ToolTipIcon.Info
                        );
                        hasShownTrayTip = true;
                    }
                    catch { }
                }
            }
            if (notifyIcon != null)
            {
                notifyIcon.Visible = true;
            }
            if (shortcutManager != null)
            {
                shortcutManager.ResetDebouncer();
            }
            isMinimizedToTray = true;
        }

        public void RestoreFromTray(bool forceMaximize = false)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(RestoreFromTray), forceMaximize);
                return;
            }

            if (shortcutManager != null)
            {
                shortcutManager.ResetDebouncer();
            }
            isMinimizedToTray = false;

            this.Show();
            this.ShowInTaskbar = true;
            if (forceMaximize)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else if (previousWindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }

            this.BringToFront();
            this.Activate();
            NativeMethods.SetForegroundWindow(this.Handle);

            // Automatically focus search bar so user can instantly start typing their search query
            this.BeginInvoke(new Action(delegate
            {
                if (txtSearch != null && txtSearch.Enabled)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                }
            }));
        }

        public void HandleSingleEscape()
        {
            if (txtSearch != null)
            {
                txtSearch.Text = string.Empty;
                if (txtSearch.Enabled)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                }
            }
            if (lvCredentials != null)
            {
                lvCredentials.SelectedItems.Clear();
            }
        }

        private void MoveSelectedItem(int delta)
        {
            if (service == null || !service.HasActiveVault || lvCredentials == null || lvCredentials.SelectedIndices.Count == 0) return;
            int curIdx = lvCredentials.SelectedIndices[0];
            int targetIdx = curIdx + delta;
            if (targetIdx < 0 || targetIdx >= lvCredentials.Items.Count) return;

            Credential cred = lvCredentials.Items[curIdx].Tag as Credential;
            if (cred == null) return;

            int targetSerialNo = targetIdx + 1;
            service.ReorderCredential(cred.Id, targetSerialNo);
            LoadCredentials();

            if (targetIdx >= 0 && targetIdx < lvCredentials.Items.Count)
            {
                lvCredentials.Items[targetIdx].Selected = true;
                lvCredentials.Items[targetIdx].EnsureVisible();
                LvCredentials_SelectedIndexChanged(lvCredentials, EventArgs.Empty);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (shortcutManager != null)
            {
                bool isSearch = txtSearch != null && txtSearch.Focused;
                bool hasSearch = txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text);
                int selLen = txtSearch != null ? txtSearch.SelectionLength : 0;
                bool hasItems = lvCredentials != null && lvCredentials.Items.Count > 0;
                bool hasSel = lvCredentials != null && lvCredentials.SelectedItems.Count > 0;
                bool isTopSel = hasSel && lvCredentials.SelectedIndices.Count > 0 && lvCredentials.SelectedIndices[0] == 0;

                if (shortcutManager.HandleCmdKey(keyData, this.ActiveControl, isSearch, hasSearch, selLen, hasItems, hasSel, isTopSel))
                {
                    return true;
                }
            }

            if (keyData == (Keys.Control | Keys.N))
            {
                PromptCreateNewVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.O))
            {
                PromptOpenExistingVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.L))
            {
                LockAndShowUnlockScreen();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private const int WM_NCHITTEST = 0x0084;

        protected override void WndProc(ref Message m)
        {
            if (hotkeyManager != null && hotkeyManager.ProcessMessage(ref m))
            {
                // Toggle is handled by the HotkeyPressed event handler; just consume the message here.
                m.Result = (IntPtr)1;
                return;
            }

            if (SingleInstanceController.RestoreWindowMessageId != 0 && m.Msg == (int)SingleInstanceController.RestoreWindowMessageId)
            {
                bool max = m.WParam == (IntPtr)2;
                RestoreFromTray(max);
                return;
            }

            if (m.Msg == WM_NCHITTEST && this.WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                if ((int)m.Result == Win32Helper.HTCLIENT)
                {
                    int x = (short)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                    Point pt = this.PointToClient(new Point(x, y));

                    // Avoid intercepting caption controls (Min, Max, Close)
                    if (!(pt.Y <= 32 && pt.X >= this.ClientSize.Width - 140))
                    {
                        int b = 6;
                        bool left = pt.X <= b;
                        bool right = pt.X >= this.ClientSize.Width - b;
                        bool top = pt.Y <= b;
                        bool bottom = pt.Y >= this.ClientSize.Height - b;

                        if (top && left) { m.Result = (IntPtr)Win32Helper.HTTOPLEFT; return; }
                        if (top && right) { m.Result = (IntPtr)Win32Helper.HTTOPRIGHT; return; }
                        if (bottom && left) { m.Result = (IntPtr)Win32Helper.HTBOTTOMLEFT; return; }
                        if (bottom && right) { m.Result = (IntPtr)Win32Helper.HTBOTTOMRIGHT; return; }
                        if (left) { m.Result = (IntPtr)Win32Helper.HTLEFT; return; }
                        if (right) { m.Result = (IntPtr)Win32Helper.HTRIGHT; return; }
                        if (top) { m.Result = (IntPtr)Win32Helper.HTTOP; return; }
                        if (bottom) { m.Result = (IntPtr)Win32Helper.HTBOTTOM; return; }
                    }
                }
                return;
            }

            base.WndProc(ref m);
        }

        public void SwitchToVaultFile(string filePath, bool updateSettings = true)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
            try
            {
                service.SwitchDatabase(filePath);
                if (updateSettings)
                {
                    AppSettings settings = AppSettings.Load();
                    settings.AddRecentVault(filePath);
                    settings.Save();
                }
                UpdateVaultStatusUI();
                LoadCredentials();
            }
            catch { }
        }

        public void LockAndShowUnlockScreen()
        {
            if (service == null || !service.HasActiveVault) return;

            string vaultPath = service.StorageFilePath;
            service.LockVault(); // Immediately wipe active keys from memory

            using (MasterPasswordForm mpf = new MasterPasswordForm(MasterPasswordMode.Unlock, vaultPath, inAppContext: true))
            {
                if (mpf.ShowDialog(this) == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(mpf.ActiveMasterPassword))
                    {
                        service.UnlockVault(mpf.ActiveMasterPassword);
                    }
                    LoadCredentials();
                    UpdateVaultStatusUI();
                }
                else
                {
                    // User canceled unlocking -> vault is locked and unloaded from active memory, stay in MainForm
                    service.SwitchDatabase(null);
                    LoadCredentials();
                    UpdateVaultStatusUI();
                }
            }
        }

        public void OpenSettingsDialog()
        {
            using (SettingsForm sf = new SettingsForm(service, AppSettings.Load()))
            {
                sf.RequestLockVault += delegate
                {
                    LockAndShowUnlockScreen();
                };
                sf.SettingsSaved += delegate
                {
                    ApplyHotkeyRegistration();
                };
                sf.ShowDialog(this);
                ApplyHotkeyRegistration();
            }
        }

        private void AttachVaultGuard(Control ctrl)
        {
            if (ctrl == null) return;
            ctrl.MouseDown += delegate
            {
                if (!ctrl.Enabled) return;
                if (service == null || !service.HasActiveVault)
                {
                    EnsureVaultLoadedForEditing();
                }
            };
            ctrl.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (!ctrl.Enabled) return;
                if (service == null || !service.HasActiveVault)
                {
                    if (!EnsureVaultLoadedForEditing())
                    {
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                    }
                }
            };
        }

        private bool EnsureVaultLoadedForEditing()
        {
            if (service != null && service.HasActiveVault)
            {
                return true;
            }

            if (isPromptingVaultAction)
            {
                return false;
            }

            try
            {
                isPromptingVaultAction = true;
                using (VaultActionDialog dlg = new VaultActionDialog())
                {
                    DialogResult res = dlg.ShowDialog(this);
                    if (res == DialogResult.Yes)
                    {
                        return PromptCreateNewVault();
                    }
                    else if (res == DialogResult.No)
                    {
                        return PromptOpenExistingVault();
                    }
                    else
                    {
                        txtSearch.Focus();
                        return false;
                    }
                }
            }
            finally
            {
                isPromptingVaultAction = false;
            }
        }

        public bool PromptCreateNewVault()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Create New KeyCraft Vault Database";
                sfd.Filter = "KeyCraft Encrypted Vault (*.kcrypt)|*.kcrypt|All Files (*.*)|*.*";
                sfd.DefaultExt = "kcrypt";
                sfd.AddExtension = true;
                sfd.FileName = "vault.kcrypt";

                string initialDir = AppSettings.GetDefaultVaultPath();
                if (!string.IsNullOrEmpty(initialDir))
                {
                    string dir = System.IO.Path.GetDirectoryName(initialDir);
                    if (System.IO.Directory.Exists(dir)) sfd.InitialDirectory = dir;
                }

                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    // User closed or cancelled save dialog -> Keep MainForm open!
                    return false;
                }

                string targetPath = sfd.FileName;

                // Prompt user to enter their master password directly (no default password)
                using (MasterPasswordForm mpf = new MasterPasswordForm(MasterPasswordMode.Create, targetPath, inAppContext: true))
                {
                    if (mpf.ShowDialog(this) == DialogResult.OK)
                    {
                        string createdPath = mpf.SelectedVaultPath;
                        service.SwitchDatabase(createdPath);
                        AppSettings.Load().AddRecentVault(createdPath);
                        LoadCredentials();
                        UpdateVaultStatusUI();
                        return true;
                    }
                    else
                    {
                        // User closed or cancelled master password setup -> Keep MainForm open!
                        return false;
                    }
                }
            }
        }

        public bool PromptOpenExistingVault()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Open KeyCraft Vault Database";
                ofd.Filter = "KeyCraft Vault (*.kcrypt;*.kdb;*.txt)|*.kcrypt;*.kdb;*.txt|All Files (*.*)|*.*";
                ofd.CheckFileExists = true;

                string initialDir = AppSettings.GetDefaultVaultPath();
                if (!string.IsNullOrEmpty(initialDir))
                {
                    string dir = System.IO.Path.GetDirectoryName(initialDir);
                    if (System.IO.Directory.Exists(dir)) ofd.InitialDirectory = dir;
                }

                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                string targetPath = ofd.FileName;
                bool isEncrypted = VaultSecurity.IsVaultEncrypted(targetPath);

                if (isEncrypted)
                {
                    using (MasterPasswordForm mpf = new MasterPasswordForm(MasterPasswordMode.Unlock, targetPath, inAppContext: true))
                    {
                        if (mpf.ShowDialog(this) == DialogResult.OK)
                        {
                            service.SwitchDatabase(targetPath);
                            AppSettings.Load().AddRecentVault(targetPath);
                            LoadCredentials();
                            UpdateVaultStatusUI();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    service.SwitchDatabase(targetPath);
                    AppSettings.Load().AddRecentVault(targetPath);
                    LoadCredentials();
                    UpdateVaultStatusUI();
                    return true;
                }
            }
        }

        private void SetInspectorEnabled(bool enabled)
        {
            if (txtSlNo != null) txtSlNo.Enabled = enabled;
            if (txtService != null) txtService.Enabled = enabled;
            if (txtUsername != null) txtUsername.Enabled = enabled;
            if (btnCopyUserField != null) btnCopyUserField.Enabled = enabled && !string.IsNullOrEmpty(txtUsername.Text);
            if (txtPassword != null) txtPassword.Enabled = enabled;
            if (btnTogglePassword != null) btnTogglePassword.Enabled = enabled;
            if (btnGeneratePassword != null) btnGeneratePassword.Enabled = enabled;
            if (txtNotes != null) txtNotes.Enabled = enabled;
            if (btnSave != null) btnSave.Enabled = enabled;
            if (btnClearForm != null) btnClearForm.Enabled = enabled;
            if (btnStepUp != null) btnStepUp.Enabled = enabled && lvCredentials != null && lvCredentials.Items.Count > 1;
            if (btnStepDown != null) btnStepDown.Enabled = enabled && lvCredentials != null && lvCredentials.Items.Count > 1;
        }

        private void BuildTableEmptyOverlay()
        {
            pnlTableOverlay = new Panel();
            pnlTableOverlay.Dock = DockStyle.Fill;
            pnlTableOverlay.BackColor = WinColors.TableBg;

            pnlOverlayCard = new Panel();
            pnlOverlayCard.Size = new Size(440, 220);
            pnlOverlayCard.BackColor = Color.FromArgb(32, 32, 32);
            pnlOverlayCard.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, pnlOverlayCard.Width - 1, pnlOverlayCard.Height - 1);
                }
                Bitmap bmp = IconResources.GetIcon("shield");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle((pnlOverlayCard.Width - 36) / 2, 22, 36, 36), WinColors.Accent);
                }
            };

            lblOverlayTitle = new Label();
            lblOverlayTitle.Text = "No Vault Loaded";
            lblOverlayTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblOverlayTitle.ForeColor = WinColors.TextWhite;
            lblOverlayTitle.Location = new Point(20, 68);
            lblOverlayTitle.Size = new Size(400, 24);
            lblOverlayTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlOverlayCard.Controls.Add(lblOverlayTitle);

            lblOverlaySub = new Label();
            lblOverlaySub.Text = "Create a new encrypted vault or open an existing .kcrypt database to view and manage credentials.";
            lblOverlaySub.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblOverlaySub.ForeColor = WinColors.TextSecondary;
            lblOverlaySub.Location = new Point(30, 96);
            lblOverlaySub.Size = new Size(380, 44);
            lblOverlaySub.TextAlign = ContentAlignment.TopCenter;
            pnlOverlayCard.Controls.Add(lblOverlaySub);

            btnOverlayNew = new ModernButton();
            btnOverlayNew.Text = "New Vault";
            btnOverlayNew.IconName = "plus";
            btnOverlayNew.IconSize = 13;
            btnOverlayNew.NormalBg = WinColors.Accent;
            btnOverlayNew.HoverBg = WinColors.AccentHover;
            btnOverlayNew.PressedBg = WinColors.AccentPressed;
            btnOverlayNew.BorderColor = WinColors.Accent;
            btnOverlayNew.NormalFg = Color.White;
            btnOverlayNew.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnOverlayNew.Size = new Size(130, 32);
            btnOverlayNew.Location = new Point(80, 154);
            btnOverlayNew.Click += delegate { PromptCreateNewVault(); };
            pnlOverlayCard.Controls.Add(btnOverlayNew);

            btnOverlayOpen = new ModernButton();
            btnOverlayOpen.Text = "Open Vault";
            btnOverlayOpen.IconName = "folder_open";
            btnOverlayOpen.IconSize = 13;
            btnOverlayOpen.Size = new Size(130, 32);
            btnOverlayOpen.Location = new Point(230, 154);
            btnOverlayOpen.Click += delegate { PromptOpenExistingVault(); };
            pnlOverlayCard.Controls.Add(btnOverlayOpen);

            btnOverlayAdd = new ModernButton();
            btnOverlayAdd.Text = "Add Credential";
            btnOverlayAdd.IconName = "plus";
            btnOverlayAdd.IconSize = 13;
            btnOverlayAdd.NormalBg = WinColors.Accent;
            btnOverlayAdd.HoverBg = WinColors.AccentHover;
            btnOverlayAdd.PressedBg = WinColors.AccentPressed;
            btnOverlayAdd.BorderColor = WinColors.Accent;
            btnOverlayAdd.NormalFg = Color.White;
            btnOverlayAdd.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnOverlayAdd.Size = new Size(160, 32);
            btnOverlayAdd.Location = new Point(140, 154);
            btnOverlayAdd.Visible = false;
            btnOverlayAdd.Click += delegate
            {
                RevertToNewCredential();
                if (txtService != null && txtService.Enabled) txtService.Focus();
            };
            pnlOverlayCard.Controls.Add(btnOverlayAdd);

            pnlTableOverlay.Controls.Add(pnlOverlayCard);
            pnlTableOverlay.Resize += delegate
            {
                pnlOverlayCard.Location = new Point(
                    Math.Max(10, (pnlTableOverlay.ClientSize.Width - pnlOverlayCard.Width) / 2),
                    Math.Max(10, (pnlTableOverlay.ClientSize.Height - pnlOverlayCard.Height) / 2)
                );
            };
        }

        private void UpdateTableEmptyState()
        {
            if (pnlTableOverlay == null) return;
            bool hasVault = service != null && service.HasActiveVault;
            if (!hasVault)
            {
                pnlTableOverlay.Visible = true;
                pnlTableOverlay.BringToFront();
                lvCredentials.Visible = false;
                lblOverlayTitle.Text = "No Vault Loaded";
                lblOverlaySub.Text = "Create a new encrypted vault or open an existing .kcrypt database to view and manage credentials.";
                btnOverlayNew.Visible = true;
                btnOverlayOpen.Visible = true;
                btnOverlayAdd.Visible = false;
            }
            else if (cachedList == null || cachedList.Count == 0)
            {
                pnlTableOverlay.Visible = true;
                pnlTableOverlay.BringToFront();
                lvCredentials.Visible = false;
                lblOverlayTitle.Text = "Vault is Empty";
                lblOverlaySub.Text = "This encrypted vault does not contain any stored credentials yet.";
                btnOverlayNew.Visible = false;
                btnOverlayOpen.Visible = false;
                btnOverlayAdd.Visible = true;
            }
            else
            {
                pnlTableOverlay.Visible = false;
                lvCredentials.Visible = true;
                lvCredentials.BringToFront();
                AdjustTableColumns();
            }
        }

        private void CopyToClipboardWithAutoClear(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            bool copied = false;
            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    Clipboard.SetText(text);
                    copied = true;
                    break;
                }
                catch
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            if (!copied)
            {
                try
                {
                    Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, text), false, 10, 50);
                }
                catch { }
            }
            lastCopiedPassword = text;

            if (clipboardClearTimer == null)
            {
                clipboardClearTimer = new Timer();
                clipboardClearTimer.Interval = 30000; // 30 seconds auto-clear
                clipboardClearTimer.Tick += delegate
                {
                    clipboardClearTimer.Stop();
                    try
                    {
                        if (Clipboard.ContainsText() && Clipboard.GetText() == lastCopiedPassword)
                        {
                            Clipboard.Clear();
                            if (lblStatusRecords != null && !this.IsDisposed)
                            {
                                lblStatusRecords.Text = "Clipboard cleared for security.";
                            }
                        }
                    }
                    catch { }
                    lastCopiedPassword = null;
                };
            }
            clipboardClearTimer.Stop();
            clipboardClearTimer.Start();
        }

        public void UpdateTitleAndVaultDisplay()
        {
            UpdateVaultStatusUI();
        }

        private void UpdateVaultStatusUI()
        {
            bool hasVault = service != null && service.HasActiveVault;
            if (hasVault)
            {
                string fileName = System.IO.Path.GetFileName(service.StorageFilePath);
                lblAppTitle.Text = "KeyCraft — [" + (string.IsNullOrEmpty(fileName) ? "vault.kcrypt" : fileName) + "]";
                this.Text = lblAppTitle.Text;
                if (lblVaultBadge != null)
                {
                    lblVaultBadge.Text = string.IsNullOrEmpty(fileName) ? "vault.kcrypt" : fileName;
                }
            }
            else
            {
                lblAppTitle.Text = "KeyCraft — [No Vault Loaded]";
                this.Text = lblAppTitle.Text;
                if (lblVaultBadge != null)
                {
                    lblVaultBadge.Text = "No Vault Loaded";
                }
            }

            if (btnLockVault != null)
            {
                btnLockVault.Enabled = hasVault;
            }

            if (txtSearch != null) txtSearch.Enabled = hasVault;
            if (btnClearSearch != null) btnClearSearch.Enabled = hasVault;
            if (cboFilter != null) cboFilter.Enabled = hasVault;

            SetInspectorEnabled(hasVault);
            UpdateTableEmptyState();
        }
    }
}
