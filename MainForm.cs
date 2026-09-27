using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PasswordGui
{
    public enum ButtonIcon
    {
        None,
        Copy,
        Delete,
        Refresh,
        Save,
        Plus,
        Eye,
        EyeOff,
        Key,
        Search,
        Shield,
        Settings,
        Lock
    }

    public enum TitleButtonType
    {
        Minimize,
        Maximize,
        Close
    }

    /// <summary>
    /// Pixel-perfect native Windows 10/11 style title bar button (Min, Max/Restore, Close).
    /// Eliminates all font baseline and alignment issues.
    /// </summary>
    public class TitleBarButton : Control
    {
        public TitleButtonType ButtonType { get; set; }
        public bool IsMaximized { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public TitleBarButton(TitleButtonType type)
        {
            this.ButtonType = type;
            this.Size = new Size(46, 40);
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Default;
            this.SetStyle(ControlStyles.Selectable, false);
            this.TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Background
            Color bg = Color.FromArgb(20, 23, 31); // Default titlebar color
            if (ButtonType == TitleButtonType.Close)
            {
                if (isPressed) bg = Color.FromArgb(190, 20, 35);
                else if (isHovered) bg = Color.FromArgb(232, 17, 35); // Native Windows Red
            }
            else
            {
                if (isPressed) bg = Color.FromArgb(45, 52, 68);
                else if (isHovered) bg = Color.FromArgb(36, 42, 56);
            }

            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillRectangle(b, this.ClientRectangle);
            }

            // Icon Pen
            Color iconColor = (ButtonType == TitleButtonType.Close && isHovered)
                ? Color.White
                : Color.FromArgb(200, 205, 215);

            int cx = this.Width / 2;
            int cy = this.Height / 2;

            using (Pen pen = new Pen(iconColor, 1f))
            {
                switch (ButtonType)
                {
                    case TitleButtonType.Minimize:
                        // Crisp 10px centered horizontal line
                        g.SmoothingMode = SmoothingMode.None;
                        g.DrawLine(pen, cx - 5, cy, cx + 5, cy);
                        break;

                    case TitleButtonType.Maximize:
                        g.SmoothingMode = SmoothingMode.None;
                        if (IsMaximized)
                        {
                            // Restore: Two overlapping squares
                            g.DrawRectangle(pen, cx - 3, cy - 5, 8, 8);
                            using (SolidBrush frontBg = new SolidBrush(bg))
                            {
                                g.FillRectangle(frontBg, cx - 5, cy - 3, 9, 9);
                            }
                            g.DrawRectangle(pen, cx - 5, cy - 3, 8, 8);
                        }
                        else
                        {
                            // Maximize: Crisp 10x10 square
                            g.DrawRectangle(pen, cx - 5, cy - 5, 10, 10);
                        }
                        break;

                    case TitleButtonType.Close:
                        // Crisp 10x10 cross
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawLine(pen, cx - 4.5f, cy - 4.5f, cx + 4.5f, cy + 4.5f);
                        g.DrawLine(pen, cx - 4.5f, cy + 4.5f, cx + 4.5f, cy - 4.5f);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Helper for drawing dynamically tinted open-source Lucide icons.
    /// </summary>
    public static class IconHelper
    {
        public static void DrawTintedIcon(Graphics g, Bitmap icon, Rectangle targetRect, Color tintColor)
        {
            if (icon == null || g == null) return;

            float r = tintColor.R / 255f;
            float gVal = tintColor.G / 255f;
            float b = tintColor.B / 255f;
            float a = tintColor.A / 255f;

            ColorMatrix cm = new ColorMatrix(new float[][] {
                new float[] { r, 0, 0, 0, 0 },
                new float[] { 0, gVal, 0, 0, 0 },
                new float[] { 0, 0, b, 0, 0 },
                new float[] { 0, 0, 0, a, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });

            using (ImageAttributes attr = new ImageAttributes())
            {
                attr.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(icon, targetRect, 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, attr);
            }
        }
    }

    /// <summary>
    /// Custom production-grade button with smooth 5px rounded corners, inset border,
    /// anti-bleed background clearing, and open-source Lucide icon pack integration.
    /// </summary>
    public class ModernButton : Button
    {
        public float CornerRadius { get; set; }
        public Color NormalBg { get; set; }
        public Color HoverBg { get; set; }
        public Color PressedBg { get; set; }
        public Color BorderColor { get; set; }
        public Color NormalFg { get; set; }
        public ButtonIcon IconType { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public ModernButton()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true
            );

            this.CornerRadius = 5.0f;
            this.NormalBg = Color.FromArgb(34, 38, 52);
            this.HoverBg = Color.FromArgb(44, 49, 66);
            this.PressedBg = Color.FromArgb(28, 31, 43);
            this.BorderColor = Color.FromArgb(51, 56, 74);
            this.NormalFg = Color.FromArgb(226, 232, 240);
            this.IconType = ButtonIcon.None;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            isPressed = false;
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;

            // 1. Clear background to Parent's solid BackColor.
            // Guarantees zero corner pixel bleeding or unpainted artifacts outside rounded border!
            Color parentBg = this.Parent != null ? this.Parent.BackColor : Color.FromArgb(24, 27, 36);
            g.Clear(parentBg);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, this.Width - 1f, this.Height - 1f);
            Color currentBg = isPressed ? PressedBg : (isHovered ? HoverBg : NormalBg);

            using (GraphicsPath path = GetRoundedRectangle(rect, CornerRadius))
            {
                using (SolidBrush bgBrush = new SolidBrush(currentBg))
                {
                    g.FillPath(bgBrush, path);
                }

                Color effectiveBorder = this.Focused ? Color.FromArgb(129, 140, 248) : BorderColor;
                if (effectiveBorder != Color.Transparent)
                {
                    using (Pen borderPen = new Pen(effectiveBorder, this.Focused ? 1.5f : 1f))
                    {
                        // Inset alignment ensures border stroke stays 100% inside rounded path
                        borderPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(borderPen, path);
                    }
                }
            }

            // Draw Lucide Icon & Text
            int iconSize = 16;
            int textX = 0;
            int contentWidth = 0;

            SizeF textSize = g.MeasureString(this.Text, this.Font);
            string iconKey = GetIconKey(IconType);
            Bitmap iconBmp = !string.IsNullOrEmpty(iconKey) ? IconResources.GetIcon(iconKey) : null;

            if (iconBmp != null)
            {
                if (string.IsNullOrEmpty(this.Text))
                {
                    contentWidth = iconSize;
                }
                else
                {
                    contentWidth = iconSize + 8 + (int)textSize.Width;
                }
            }
            else
            {
                contentWidth = (int)textSize.Width;
            }

            int startX = (this.Width - contentWidth) / 2;
            int startY = (this.Height - iconSize) / 2;

            if (iconBmp != null)
            {
                Rectangle iconRect = new Rectangle(startX, startY, iconSize, iconSize);
                IconHelper.DrawTintedIcon(g, iconBmp, iconRect, NormalFg);
                textX = startX + iconSize + 8;
            }
            else
            {
                textX = startX;
            }

            if (!string.IsNullOrEmpty(this.Text))
            {
                int textY = (this.Height - (int)textSize.Height) / 2;
                using (SolidBrush textBrush = new SolidBrush(NormalFg))
                {
                    g.DrawString(this.Text, this.Font, textBrush, textX, textY);
                }
            }
        }

        private static string GetIconKey(ButtonIcon icon)
        {
            switch (icon)
            {
                case ButtonIcon.Copy: return "copy";
                case ButtonIcon.Delete: return "trash";
                case ButtonIcon.Refresh: return "refresh";
                case ButtonIcon.Save: return "save";
                case ButtonIcon.Plus: return "plus";
                case ButtonIcon.Eye: return "eye";
                case ButtonIcon.EyeOff: return "eye_off";
                case ButtonIcon.Key: return "key";
                case ButtonIcon.Search: return "search";
                case ButtonIcon.Shield: return "shield";
                case ButtonIcon.Settings: return "settings";
                case ButtonIcon.Lock: return "lock";
                default: return null;
            }
        }

        private static GraphicsPath GetRoundedRectangle(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Forwards edge hit-tests from child controls to parent MainForm for seamless border resizing.
    /// </summary>
    public class EdgeResizeFilter : NativeWindow
    {
        private readonly Form targetForm;
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;
        private const int BorderMargin = 8;

        public EdgeResizeFilter(Control ctrl, Form form)
        {
            this.targetForm = form;
            if (ctrl.IsHandleCreated)
            {
                this.AssignHandle(ctrl.Handle);
            }
            else
            {
                ctrl.HandleCreated += delegate { this.AssignHandle(ctrl.Handle); };
            }
            ctrl.HandleDestroyed += delegate { this.ReleaseHandle(); };
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && targetForm.WindowState == FormWindowState.Normal)
            {
                Point screenPt = new Point(m.LParam.ToInt32());
                Point clientPt = targetForm.PointToClient(screenPt);

                // Do not intercept titlebar buttons (close/max/min)
                bool inTitleButtons = (clientPt.X >= targetForm.ClientSize.Width - 140 && clientPt.Y <= 42);

                if (!inTitleButtons)
                {
                    if (clientPt.X <= BorderMargin || clientPt.X >= targetForm.ClientSize.Width - BorderMargin ||
                        clientPt.Y <= BorderMargin || clientPt.Y >= targetForm.ClientSize.Height - BorderMargin)
                    {
                        m.Result = (IntPtr)HTTRANSPARENT;
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Production-grade GUI with guaranteed top-level titlebar placement,
    /// pixel-perfect native Windows 10/11 title buttons, 3.2px rounded action buttons,
    /// and Obsidian/Indigo theme.
    /// </summary>
    public class MainForm : Form
    {
        // Core Logic & Database
        private readonly CredentialService service;
        private List<Credential> cachedList;
        private Credential selectedCredential = null;

        // Custom Title Bar Controls
        private Panel panelTitleBar;
        private Label lblAppTitle;
        private TitleBarButton btnMin;
        private TitleBarButton btnMax;
        private TitleBarButton btnClose;

        // Header & Content Panels
        private Panel panelMain;
        private Panel panelHeader;
        private Panel panelContent;

        // Left Panel (Directory & Search)
        private Panel panelSearch;
        private TextBox txtSearch;
        private ComboBox cmbSearchColumn;
        private bool isSearchFocused = false;
        private ListView lvCredentials;
        private bool isAdjustingColumns = false;
        private ModernButton btnCopyPassword;
        private ModernButton btnDelete;
        private ModernButton btnRefresh;
        private ModernButton btnSettings;

        // Vault Toolbar Controls (KeePass Style)
        private Label lblVaultBadge;
        private ModernButton btnNewVault;
        private ModernButton btnOpenVault;
        private ModernButton btnLockVault;

        // System Tray & Keyboard Routing
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayMenu;
        private bool hasShownTrayTip = false;
        private KeyboardShortcutManager shortcutManager;
        private AppSettings appSettings;
        private GlobalHotkeyManager hotkeyManager;

        // Right Panel (Editor)
        private Label lblEditorHeader;
        private TextBox txtSerialNo;
        private ModernButton btnMoveUp;
        private ModernButton btnMoveDown;
        private TextBox txtService;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private ModernButton btnTogglePassword;
        private ModernButton btnGeneratePassword;
        private Label lblStrengthStatus;
        private ModernButton btnSave;
        private ModernButton btnClear;
        private Label lblToast;
        private Timer timerToast;

        // Status Bar
        private Panel panelStatusBar;
        private Label lblStatusFile;
        private Label lblStatusCount;

        // ==========================================
        // PRODUCTION OBSIDIAN & INDIGO PALETTE
        // ==========================================
        private static readonly Color ColorBgWindow = Color.FromArgb(14, 16, 21);        // #0E1015
        private static readonly Color ColorTitleBar = Color.FromArgb(20, 23, 31);        // #14171F
        private static readonly Color ColorBgCard = Color.FromArgb(24, 27, 36);          // #181B24
        private static readonly Color ColorBgInput = Color.FromArgb(31, 35, 48);         // #1F2330
        private static readonly Color ColorBorder = Color.FromArgb(40, 45, 60);          // #282D3C

        // Indigo Action Colors
        private static readonly Color ColorPrimary = Color.FromArgb(79, 70, 229);        // #4F46E5
        private static readonly Color ColorPrimaryHover = Color.FromArgb(99, 102, 241);   // #6366F1
        private static readonly Color ColorPrimaryPressed = Color.FromArgb(67, 56, 202); // #4338CA

        // Secondary Button Colors
        private static readonly Color ColorSecondary = Color.FromArgb(34, 38, 52);       // #222634
        private static readonly Color ColorSecondaryHover = Color.FromArgb(44, 49, 66);   // #2C3142
        private static readonly Color ColorSecondaryBorder = Color.FromArgb(51, 56, 74); // #33384A

        // Copy Button (Cyan Accent)
        private static readonly Color ColorCopyBg = Color.FromArgb(22, 38, 54);          // #162636
        private static readonly Color ColorCopyHover = Color.FromArgb(28, 49, 70);       // #1C3146
        private static readonly Color ColorCopyBorder = Color.FromArgb(35, 78, 110);     // #234E6E
        private static readonly Color ColorCopyText = Color.FromArgb(56, 189, 248);      // #38BDF8

        // Danger Button (Crimson Accent)
        private static readonly Color ColorDangerBg = Color.FromArgb(45, 24, 28);        // #2D181C
        private static readonly Color ColorDangerHover = Color.FromArgb(62, 31, 37);     // #3E1F25
        private static readonly Color ColorDangerBorder = Color.FromArgb(127, 29, 29);   // #7F1D1D
        private static readonly Color ColorDangerText = Color.FromArgb(248, 113, 113);   // #F87171

        // Update/Warning Button
        private static readonly Color ColorWarningBg = Color.FromArgb(217, 119, 6);      // #D97706
        private static readonly Color ColorWarningHover = Color.FromArgb(245, 158, 11);  // #F59E0B

        // Typography
        private static readonly Color ColorTextPrimary = Color.FromArgb(241, 245, 249);  // #F1F5F9
        private static readonly Color ColorTextMuted = Color.FromArgb(148, 163, 184);    // #94A3B8
        private static readonly Color ColorSuccess = Color.FromArgb(52, 211, 153);       // #34D399

        private Font fontRegular;
        private Font fontBold;
        private Font fontMono;

        private bool isPasswordRevealed = false;

        // ==========================================
        // WIN32 DRAGGING & RESIZING
        // ==========================================
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        public MainForm(CredentialService credService)
        {
            if (credService == null) throw new ArgumentNullException("credService");
            this.service = credService;

            fontRegular = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            fontBold = new Font("Segoe UI", 9.25f, FontStyle.Bold);
            fontMono = new Font("Consolas", 10.5f, FontStyle.Regular);

            appSettings = AppSettings.Load();
            hotkeyManager = new GlobalHotkeyManager();
            hotkeyManager.HotkeyPressed += delegate { RestoreFromTray(); };

            InitializeComponent();
            UpdateTitleAndVaultDisplay();
            LoadCredentials();
            this.ActiveControl = txtSearch;
        }

        private void InitializeComponent()
        {
            this.Text = "KeyCraft - Credential & Password Manager";
            this.Size = new Size(980, 660);
            this.MinimumSize = new Size(860, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBgWindow;
            this.ForeColor = ColorTextPrimary;
            this.Font = fontRegular;
            this.FormBorderStyle = FormBorderStyle.None; // Headless frameless window
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            // =========================================================================
            // 1. ABSOLUTE TOP: CUSTOM TITLE BAR (EXACT POSITION Y = 0, HEIGHT = 40)
            // =========================================================================
            panelTitleBar = new Panel();
            panelTitleBar.Dock = DockStyle.Top;
            panelTitleBar.Height = 40;
            panelTitleBar.BackColor = ColorTitleBar;
            panelTitleBar.MouseDown += new MouseEventHandler(TitleBar_MouseDown);
            panelTitleBar.DoubleClick += new EventHandler(TitleBar_DoubleClick);

            // App Icon Graphic (Official Lucide Shield-Check icon vertically centered at Y = 12)
            Panel panelAppIcon = new Panel();
            panelAppIcon.Location = new Point(14, 12);
            panelAppIcon.Size = new Size(16, 16);
            panelAppIcon.BackColor = Color.Transparent;
            panelAppIcon.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap shieldBmp = IconResources.GetIcon("shield");
                if (shieldBmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, shieldBmp, new Rectangle(0, 0, 16, 16), ColorPrimaryHover);
                }
            };
            panelAppIcon.MouseDown += new MouseEventHandler(TitleBar_MouseDown);
            panelTitleBar.Controls.Add(panelAppIcon);

            // Title Bar Text (vertically centered at Y = 11)
            lblAppTitle = new Label();
            lblAppTitle.Text = "KeyCraft  —  Credential & Password Manager";
            lblAppTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblAppTitle.ForeColor = ColorTextMuted;
            lblAppTitle.Location = new Point(38, 11);
            lblAppTitle.AutoSize = true;
            lblAppTitle.MouseDown += new MouseEventHandler(TitleBar_MouseDown);
            panelTitleBar.Controls.Add(lblAppTitle);

            // Native Windows 10/11 Title Bar Buttons Container (Width 138, Height 40)
            // Explicit coordinates guarantee correct Windows order: [Minimize] [Maximize] [Close]
            Panel panelButtons = new Panel();
            panelButtons.Dock = DockStyle.Right;
            panelButtons.Size = new Size(138, 40);
            panelButtons.BackColor = Color.Transparent;

            btnMin = new TitleBarButton(TitleButtonType.Minimize);
            btnMin.Location = new Point(0, 0);
            btnMin.Size = new Size(46, 40);
            btnMin.Click += delegate { MinimizeToTray(); };
            panelButtons.Controls.Add(btnMin);

            btnMax = new TitleBarButton(TitleButtonType.Maximize);
            btnMax.Location = new Point(46, 0);
            btnMax.Size = new Size(46, 40);
            btnMax.Click += delegate { ToggleMaximize(); };
            panelButtons.Controls.Add(btnMax);

            btnClose = new TitleBarButton(TitleButtonType.Close);
            btnClose.Location = new Point(92, 0);
            btnClose.Size = new Size(46, 40);
            btnClose.Click += delegate { this.Close(); };
            panelButtons.Controls.Add(btnClose);

            panelTitleBar.Controls.Add(panelButtons);

            // Title Bar Settings Button (Placed directly to the left of window control buttons)
            Panel btnTitleSettings = new Panel();
            btnTitleSettings.Dock = DockStyle.Right;
            btnTitleSettings.Size = new Size(40, 40);
            btnTitleSettings.Cursor = Cursors.Hand;
            bool isTitleSettingsHovered = false;
            btnTitleSettings.MouseEnter += delegate { isTitleSettingsHovered = true; btnTitleSettings.Invalidate(); };
            btnTitleSettings.MouseLeave += delegate { isTitleSettingsHovered = false; btnTitleSettings.Invalidate(); };
            btnTitleSettings.Paint += delegate(object s, PaintEventArgs pe)
            {
                if (isTitleSettingsHovered)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(28, 33, 46)))
                    {
                        pe.Graphics.FillRectangle(b, btnTitleSettings.ClientRectangle);
                    }
                }
                Bitmap bmp = IconResources.GetIcon("settings");
                if (bmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(12, 12, 16, 16), isTitleSettingsHovered ? ColorPrimaryHover : ColorTextMuted);
                }
            };
            btnTitleSettings.Click += delegate { OpenSettings(); };
            panelTitleBar.Controls.Add(btnTitleSettings);

            // 1px Subtle Divider under Title Bar
            Panel titleDivider = new Panel();
            titleDivider.Dock = DockStyle.Bottom;
            titleDivider.Height = 1;
            titleDivider.BackColor = ColorBorder;
            panelTitleBar.Controls.Add(titleDivider);

            // =========================================================================
            // 2. BOTTOM: SLEEK STATUS BAR
            // =========================================================================
            panelStatusBar = new Panel();
            panelStatusBar.Dock = DockStyle.Bottom;
            panelStatusBar.Height = 28;
            panelStatusBar.BackColor = ColorTitleBar;
            panelStatusBar.Padding = new Padding(16, 4, 16, 4);

            Panel statusDivider = new Panel();
            statusDivider.Dock = DockStyle.Top;
            statusDivider.Height = 1;
            statusDivider.BackColor = ColorBorder;
            panelStatusBar.Controls.Add(statusDivider);

            lblStatusFile = new Label();
            lblStatusFile.Text = "● Encrypted Vault: " + System.IO.Path.GetFileName(service.StorageFilePath) + " (AES-256)";
            lblStatusFile.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            lblStatusFile.ForeColor = ColorSuccess;
            lblStatusFile.Location = new Point(14, 6);
            lblStatusFile.AutoSize = true;
            panelStatusBar.Controls.Add(lblStatusFile);

            lblStatusCount = new Label();
            lblStatusCount.Text = "Total: 0 credentials";
            lblStatusCount.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            lblStatusCount.ForeColor = ColorTextMuted;
            lblStatusCount.Dock = DockStyle.Right;
            lblStatusCount.TextAlign = ContentAlignment.MiddleRight;
            lblStatusCount.Padding = new Padding(0, 4, 14, 0);
            panelStatusBar.Controls.Add(lblStatusCount);

            // =========================================================================
            // 3. MIDDLE: MAIN PANEL (CONTAINS HEADER & CONTENT)
            // =========================================================================
            panelMain = new Panel();
            panelMain.Dock = DockStyle.Fill;
            panelMain.BackColor = ColorBgWindow;

            // HEADER (NESTED INSIDE MAIN - IMPOSSIBLE TO BE ABOVE TITLEBAR)
            panelHeader = new Panel();
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 64;
            panelHeader.BackColor = ColorBgWindow;
            panelHeader.Padding = new Padding(24, 10, 24, 8);

            Label lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "Saved Credentials";
            lblHeaderTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblHeaderTitle.ForeColor = ColorTextPrimary;
            lblHeaderTitle.Location = new Point(24, 8);
            lblHeaderTitle.AutoSize = true;
            panelHeader.Controls.Add(lblHeaderTitle);

            Label lblHeaderSubtitle = new Label();
            lblHeaderSubtitle.Text = "Store, update, and inspect your account credentials safely.";
            lblHeaderSubtitle.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            lblHeaderSubtitle.ForeColor = ColorTextMuted;
            lblHeaderSubtitle.Location = new Point(26, 34);
            lblHeaderSubtitle.AutoSize = true;
            panelHeader.Controls.Add(lblHeaderSubtitle);

            // Vault Toolbar on Header Right (KeePass Style)
            Panel pnlVaultHeader = new Panel();
            pnlVaultHeader.Dock = DockStyle.Right;
            pnlVaultHeader.Width = 470;
            pnlVaultHeader.Height = 44;
            pnlVaultHeader.BackColor = Color.Transparent;

            // Active Vault Pill Label
            lblVaultBadge = new Label();
            lblVaultBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblVaultBadge.ForeColor = ColorPrimaryHover;
            lblVaultBadge.BackColor = ColorBgCard;
            lblVaultBadge.Location = new Point(0, 14);
            lblVaultBadge.Size = new Size(160, 32);
            lblVaultBadge.TextAlign = ContentAlignment.MiddleCenter;
            lblVaultBadge.Cursor = Cursors.Hand;
            lblVaultBadge.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, lblVaultBadge.Width - 1, lblVaultBadge.Height - 1);
                }
            };
            lblVaultBadge.Click += delegate { OpenVault(); };
            pnlVaultHeader.Controls.Add(lblVaultBadge);

            // New Vault Button (Ctrl+N)
            btnNewVault = new ModernButton();
            btnNewVault.Text = "New Vault";
            btnNewVault.IconType = ButtonIcon.Plus;
            btnNewVault.Location = new Point(168, 14);
            btnNewVault.Size = new Size(95, 32);
            btnNewVault.CornerRadius = 4.0f;
            btnNewVault.NormalBg = ColorSecondary;
            btnNewVault.HoverBg = ColorSecondaryHover;
            btnNewVault.BorderColor = ColorSecondaryBorder;
            btnNewVault.NormalFg = ColorTextPrimary;
            btnNewVault.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnNewVault.Click += delegate { NewVault(); };
            pnlVaultHeader.Controls.Add(btnNewVault);

            // Open Vault Button (Ctrl+O)
            btnOpenVault = new ModernButton();
            btnOpenVault.Text = "Open...";
            btnOpenVault.IconType = ButtonIcon.Search;
            btnOpenVault.Location = new Point(270, 14);
            btnOpenVault.Size = new Size(92, 32);
            btnOpenVault.CornerRadius = 4.0f;
            btnOpenVault.NormalBg = ColorSecondary;
            btnOpenVault.HoverBg = ColorSecondaryHover;
            btnOpenVault.BorderColor = ColorSecondaryBorder;
            btnOpenVault.NormalFg = ColorTextPrimary;
            btnOpenVault.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnOpenVault.Click += delegate { OpenVault(); };
            pnlVaultHeader.Controls.Add(btnOpenVault);

            // Lock Vault Button (Ctrl+L)
            btnLockVault = new ModernButton();
            btnLockVault.Text = "Lock";
            btnLockVault.IconType = ButtonIcon.Lock;
            btnLockVault.Location = new Point(368, 14);
            btnLockVault.Size = new Size(82, 32);
            btnLockVault.CornerRadius = 4.0f;
            btnLockVault.NormalBg = ColorSecondary;
            btnLockVault.HoverBg = ColorSecondaryHover;
            btnLockVault.BorderColor = ColorSecondaryBorder;
            btnLockVault.NormalFg = ColorWarningBg;
            btnLockVault.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnLockVault.Click += delegate { LockVault(); };
            pnlVaultHeader.Controls.Add(btnLockVault);

            panelHeader.Controls.Add(pnlVaultHeader);

            panelMain.Controls.Add(panelHeader);

            // CONTENT CONTAINER
            panelContent = new Panel();
            panelContent.Dock = DockStyle.Fill;
            panelContent.BackColor = ColorBgWindow;
            panelContent.Padding = new Padding(24, 4, 24, 12);
            panelMain.Controls.Add(panelContent);
            panelContent.BringToFront();

            // ------------------------------------------
            // RIGHT PANEL: Credential Editor Card
            // ------------------------------------------
            Panel panelRight = new Panel();
            panelRight.Dock = DockStyle.Right;
            panelRight.Width = 350;
            panelRight.BackColor = ColorBgCard;
            panelRight.Padding = new Padding(20);
            panelRight.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen borderPen = new Pen(ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(borderPen, 0, 0, panelRight.Width - 1, panelRight.Height - 1);
                }
            };
            panelContent.Controls.Add(panelRight);

            lblEditorHeader = new Label();
            lblEditorHeader.Text = "New Credential";
            lblEditorHeader.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblEditorHeader.ForeColor = ColorTextPrimary;
            lblEditorHeader.Location = new Point(18, 14);
            lblEditorHeader.AutoSize = true;
            panelRight.Controls.Add(lblEditorHeader);

            panelRight.TabStop = false;

            // Field: Serial Number (Remapping & Reordering)
            Label lblSerial = CreateFieldLabel("SL NO", 18, 44);
            panelRight.Controls.Add(lblSerial);

            txtSerialNo = CreateInputTextBox(18, 64, 60);
            txtSerialNo.TabIndex = 4;
            txtSerialNo.TabStop = true;
            panelRight.Controls.Add(txtSerialNo);

            btnMoveUp = new ModernButton();
            btnMoveUp.Text = "▲";
            btnMoveUp.Location = new Point(84, 64);
            btnMoveUp.Size = new Size(36, 26);
            btnMoveUp.CornerRadius = 4.0f;
            btnMoveUp.NormalBg = ColorSecondary;
            btnMoveUp.HoverBg = ColorSecondaryHover;
            btnMoveUp.BorderColor = ColorSecondaryBorder;
            btnMoveUp.NormalFg = ColorTextPrimary;
            btnMoveUp.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.TabStop = true;
            btnMoveUp.Click += new EventHandler(BtnMoveUp_Click);
            panelRight.Controls.Add(btnMoveUp);

            btnMoveDown = new ModernButton();
            btnMoveDown.Text = "▼";
            btnMoveDown.Location = new Point(126, 64);
            btnMoveDown.Size = new Size(36, 26);
            btnMoveDown.CornerRadius = 4.0f;
            btnMoveDown.NormalBg = ColorSecondary;
            btnMoveDown.HoverBg = ColorSecondaryHover;
            btnMoveDown.BorderColor = ColorSecondaryBorder;
            btnMoveDown.NormalFg = ColorTextPrimary;
            btnMoveDown.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.TabStop = true;
            btnMoveDown.Click += new EventHandler(BtnMoveDown_Click);
            panelRight.Controls.Add(btnMoveDown);

            // Field: Service
            Label lblService = CreateFieldLabel("SERVICE / WEBSITE *", 18, 98);
            panelRight.Controls.Add(lblService);

            txtService = CreateInputTextBox(18, 120, 310);
            txtService.TabIndex = 7;
            txtService.TabStop = true;
            panelRight.Controls.Add(txtService);

            // Field: Username
            Label lblUser = CreateFieldLabel("USERNAME / EMAIL", 18, 156);
            panelRight.Controls.Add(lblUser);

            txtUsername = CreateInputTextBox(18, 178, 310);
            txtUsername.TabIndex = 8;
            txtUsername.TabStop = true;
            panelRight.Controls.Add(txtUsername);

            // Field: Password
            Label lblPwd = CreateFieldLabel("PASSWORD *", 18, 214);
            panelRight.Controls.Add(lblPwd);

            txtPassword = CreateInputTextBox(18, 236, 226);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Font = fontMono;
            txtPassword.TabIndex = 9;
            txtPassword.TabStop = true;
            txtPassword.TextChanged += new EventHandler(TxtPassword_TextChanged);
            panelRight.Controls.Add(txtPassword);

            // Toggle Password Button (5.0px rounded with Lucide Eye Icon)
            btnTogglePassword = new ModernButton();
            btnTogglePassword.Location = new Point(250, 236);
            btnTogglePassword.Size = new Size(36, 26);
            btnTogglePassword.CornerRadius = 5.0f;
            btnTogglePassword.IconType = ButtonIcon.Eye;
            btnTogglePassword.NormalBg = ColorSecondary;
            btnTogglePassword.HoverBg = ColorSecondaryHover;
            btnTogglePassword.BorderColor = ColorSecondaryBorder;
            btnTogglePassword.NormalFg = ColorTextMuted;
            btnTogglePassword.TabIndex = 10;
            btnTogglePassword.TabStop = true;
            btnTogglePassword.Click += new EventHandler(BtnTogglePassword_Click);
            panelRight.Controls.Add(btnTogglePassword);

            // Generate Random Password Button (5.0px rounded with Lucide Key Icon)
            btnGeneratePassword = new ModernButton();
            btnGeneratePassword.Location = new Point(292, 236);
            btnGeneratePassword.Size = new Size(36, 26);
            btnGeneratePassword.CornerRadius = 5.0f;
            btnGeneratePassword.IconType = ButtonIcon.Key;
            btnGeneratePassword.NormalBg = ColorPrimary;
            btnGeneratePassword.HoverBg = ColorPrimaryHover;
            btnGeneratePassword.BorderColor = ColorPrimary;
            btnGeneratePassword.NormalFg = Color.White;
            btnGeneratePassword.TabIndex = 11;
            btnGeneratePassword.TabStop = true;
            btnGeneratePassword.Click += new EventHandler(BtnGeneratePassword_Click);
            panelRight.Controls.Add(btnGeneratePassword);

            // Password Strength Status
            lblStrengthStatus = new Label();
            lblStrengthStatus.Text = "Strength: None";
            lblStrengthStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblStrengthStatus.ForeColor = ColorTextMuted;
            lblStrengthStatus.Location = new Point(18, 268);
            lblStrengthStatus.AutoSize = true;
            panelRight.Controls.Add(lblStrengthStatus);

            // Save Credential Button (5.0px rounded with Lucide Save Icon)
            btnSave = new ModernButton();
            btnSave.Text = "Save Credential";
            btnSave.Location = new Point(18, 298);
            btnSave.Size = new Size(310, 38);
            btnSave.CornerRadius = 5.0f;
            btnSave.IconType = ButtonIcon.Save;
            btnSave.NormalBg = ColorPrimary;
            btnSave.HoverBg = ColorPrimaryHover;
            btnSave.PressedBg = ColorPrimaryPressed;
            btnSave.BorderColor = ColorPrimaryHover;
            btnSave.NormalFg = Color.White;
            btnSave.Font = fontBold;
            btnSave.TabIndex = 12;
            btnSave.TabStop = true;
            btnSave.Click += new EventHandler(BtnSave_Click);
            panelRight.Controls.Add(btnSave);

            // Clear Form Button (5.0px rounded with Lucide Plus Icon)
            btnClear = new ModernButton();
            btnClear.Text = "New / Clear Form";
            btnClear.Location = new Point(18, 344);
            btnClear.Size = new Size(310, 32);
            btnClear.CornerRadius = 5.0f;
            btnClear.IconType = ButtonIcon.Plus;
            btnClear.NormalBg = ColorSecondary;
            btnClear.HoverBg = ColorSecondaryHover;
            btnClear.PressedBg = Color.FromArgb(26, 29, 40);
            btnClear.BorderColor = ColorSecondaryBorder;
            btnClear.NormalFg = ColorTextMuted;
            btnClear.Font = fontRegular;
            btnClear.TabIndex = 13;
            btnClear.TabStop = true;
            btnClear.Click += new EventHandler(BtnClear_Click);
            panelRight.Controls.Add(btnClear);

            // Toast feedback label
            lblToast = new Label();
            lblToast.Text = "✓ Saved successfully!";
            lblToast.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblToast.ForeColor = ColorSuccess;
            lblToast.Location = new Point(18, 386);
            lblToast.AutoSize = true;
            lblToast.Visible = false;
            panelRight.Controls.Add(lblToast);

            timerToast = new Timer();
            timerToast.Interval = 2500;
            timerToast.Tick += new EventHandler(TimerToast_Tick);

            // ------------------------------------------
            // LEFT PANEL: Directory, Search & Actions
            // ------------------------------------------
            Panel panelLeft = new Panel();
            panelLeft.Dock = DockStyle.Fill;
            panelLeft.BackColor = ColorBgWindow;
            panelLeft.Padding = new Padding(0, 0, 18, 0);
            panelContent.Controls.Add(panelLeft);
            panelLeft.BringToFront();

            // Search Bar Container (Integrated input bar: panel -> icon -> borderless textbox -> column dropdown)
            panelSearch = new Panel();
            panelSearch.Dock = DockStyle.Top;
            panelSearch.Height = 34;
            panelSearch.BackColor = ColorBgInput;
            panelSearch.Padding = new Padding(2, 2, 2, 2);
            panelSearch.Cursor = Cursors.Default;
            panelSearch.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen borderPen = new Pen(isSearchFocused ? ColorPrimary : ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(borderPen, 0, 0, panelSearch.Width - 1, panelSearch.Height - 1);
                }
            };

            // Lucide Search Icon (16x16 inside search bar)
            Panel panelSearchIcon = new Panel();
            panelSearchIcon.Size = new Size(16, 16);
            panelSearchIcon.Location = new Point(4, (panelSearch.Height - 16) / 2);
            panelSearchIcon.BackColor = Color.Transparent;
            panelSearchIcon.Cursor = Cursors.Default;
            panelSearchIcon.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap searchBmp = IconResources.GetIcon("search");
                if (searchBmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, searchBmp, new Rectangle(0, 0, 16, 16), isSearchFocused ? ColorPrimaryHover : ColorTextMuted);
                }
            };
            panelSearch.Controls.Add(panelSearchIcon);

            // Column selector dropdown on the right side of the search bar (Default: "Sl No")
            cmbSearchColumn = new ComboBox();
            cmbSearchColumn.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSearchColumn.FlatStyle = FlatStyle.Flat;
            cmbSearchColumn.BackColor = ColorBgInput;
            cmbSearchColumn.ForeColor = ColorTextPrimary;
            cmbSearchColumn.Cursor = Cursors.Default;
            cmbSearchColumn.Font = new Font("Segoe UI", 9f);
            cmbSearchColumn.Items.AddRange(new object[] { "Sl No", "Service", "Username", "All Columns" });
            cmbSearchColumn.SelectedIndex = 0; // Default: Sl No
            int cmbWidth = 110;
            cmbSearchColumn.Size = new Size(cmbWidth, 24);
            cmbSearchColumn.Location = new Point(Math.Max(50, panelSearch.ClientSize.Width - cmbWidth - 4), (panelSearch.Height - 24) / 2);
            cmbSearchColumn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbSearchColumn.SelectedIndexChanged += delegate { ApplyFilter(); };
            panelSearch.Controls.Add(cmbSearchColumn);

            // Input Box without borders:
            int inputLeft = 4 + 16 + 6; // 26px
            int inputTop = 8;
            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = ColorBgInput;
            txtSearch.ForeColor = ColorTextPrimary;
            txtSearch.Cursor = Cursors.IBeam;
            txtSearch.Font = new Font("Segoe UI", 9.5f);
            txtSearch.TabIndex = 0;
            txtSearch.TabStop = true;
            txtSearch.Location = new Point(inputLeft, inputTop);
            txtSearch.Size = new Size(Math.Max(50, panelSearch.ClientSize.Width - inputLeft - cmbWidth - 14), 18);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panelSearch.Resize += delegate
            {
                txtSearch.Width = Math.Max(50, panelSearch.ClientSize.Width - inputLeft - cmbWidth - 14);
                cmbSearchColumn.Left = Math.Max(50, panelSearch.ClientSize.Width - cmbWidth - 4);
            };

            // Highlight outer panel when clicking / focusing the input box
            txtSearch.GotFocus += delegate
            {
                isSearchFocused = true;
                panelSearch.Invalidate();
                panelSearchIcon.Invalidate();
            };
            txtSearch.LostFocus += delegate
            {
                isSearchFocused = false;
                panelSearch.Invalidate();
                panelSearchIcon.Invalidate();
            };
            txtSearch.TextChanged += new EventHandler(TxtSearch_TextChanged);

            txtSearch.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (lvCredentials.SelectedItems.Count > 0)
                    {
                        txtService.Focus();
                        txtService.SelectAll();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.C && e.Control)
                {
                    if (txtSearch.SelectionLength == 0 && lvCredentials.SelectedItems.Count > 0)
                    {
                        btnCopyPassword.PerformClick();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (lvCredentials.Items.Count > 0)
                    {
                        lvCredentials.Focus();
                        if (lvCredentials.SelectedItems.Count == 0)
                        {
                            lvCredentials.Items[0].Selected = true;
                        }
                        e.Handled = true;
                    }
                }
            };

            // Clicking outer panel or icon focuses the input box
            panelSearch.MouseDown += delegate { txtSearch.Focus(); };
            panelSearchIcon.MouseDown += delegate { txtSearch.Focus(); };

            panelSearch.Controls.Add(txtSearch);
            panelLeft.Controls.Add(panelSearch);

            // Bottom Action Buttons Row (Rounded 5.0px, Lucide Icons, Generous Widths)
            Panel panelLeftBottom = new Panel();
            panelLeftBottom.Dock = DockStyle.Bottom;
            panelLeftBottom.Height = 46;
            panelLeftBottom.BackColor = ColorBgWindow;
            panelLeftBottom.Padding = new Padding(0, 10, 0, 0);
            panelLeftBottom.TabStop = false;

            btnCopyPassword = new ModernButton();
            btnCopyPassword.Text = "Copy Password";
            btnCopyPassword.Location = new Point(0, 8);
            btnCopyPassword.Size = new Size(150, 34);
            btnCopyPassword.CornerRadius = 5.0f;
            btnCopyPassword.IconType = ButtonIcon.Copy;
            btnCopyPassword.NormalBg = ColorCopyBg;
            btnCopyPassword.HoverBg = ColorCopyHover;
            btnCopyPassword.BorderColor = ColorCopyBorder;
            btnCopyPassword.NormalFg = ColorCopyText;
            btnCopyPassword.TabIndex = 2;
            btnCopyPassword.TabStop = true;
            btnCopyPassword.Click += new EventHandler(BtnCopyPassword_Click);
            panelLeftBottom.Controls.Add(btnCopyPassword);

            btnDelete = new ModernButton();
            btnDelete.Text = "Delete";
            btnDelete.Location = new Point(158, 8);
            btnDelete.Size = new Size(100, 34);
            btnDelete.CornerRadius = 5.0f;
            btnDelete.IconType = ButtonIcon.Delete;
            btnDelete.NormalBg = ColorDangerBg;
            btnDelete.HoverBg = ColorDangerHover;
            btnDelete.BorderColor = ColorDangerBorder;
            btnDelete.NormalFg = ColorDangerText;
            btnDelete.TabIndex = 3;
            btnDelete.TabStop = true;
            btnDelete.Click += new EventHandler(BtnDelete_Click);
            panelLeftBottom.Controls.Add(btnDelete);

            btnRefresh = new ModernButton();
            btnRefresh.Text = "Refresh";
            btnRefresh.Location = new Point(266, 8);
            btnRefresh.Size = new Size(100, 34);
            btnRefresh.CornerRadius = 5.0f;
            btnRefresh.IconType = ButtonIcon.Refresh;
            btnRefresh.NormalBg = ColorSecondary;
            btnRefresh.HoverBg = ColorSecondaryHover;
            btnRefresh.BorderColor = ColorSecondaryBorder;
            btnRefresh.NormalFg = ColorTextMuted;
            btnRefresh.TabIndex = 4;
            btnRefresh.TabStop = true;
            btnRefresh.Click += new EventHandler(BtnRefresh_Click);
            panelLeftBottom.Controls.Add(btnRefresh);

            btnSettings = new ModernButton();
            btnSettings.Text = "Settings";
            btnSettings.Location = new Point(374, 8);
            btnSettings.Size = new Size(105, 34);
            btnSettings.CornerRadius = 5.0f;
            btnSettings.IconType = ButtonIcon.Settings;
            btnSettings.NormalBg = ColorSecondary;
            btnSettings.HoverBg = ColorSecondaryHover;
            btnSettings.BorderColor = ColorSecondaryBorder;
            btnSettings.NormalFg = ColorTextMuted;
            btnSettings.TabIndex = 5;
            btnSettings.TabStop = true;
            btnSettings.Click += delegate { OpenSettings(); };
            panelLeftBottom.Controls.Add(btnSettings);

            panelLeft.Controls.Add(panelLeftBottom);

            // ListView with Obsidian Card Styling
            lvCredentials = new ListView();
            lvCredentials.Dock = DockStyle.Fill;
            lvCredentials.View = View.Details;
            lvCredentials.FullRowSelect = true;
            lvCredentials.MultiSelect = false;
            lvCredentials.HideSelection = false;
            lvCredentials.BackColor = ColorBgCard;
            lvCredentials.ForeColor = ColorTextPrimary;
            lvCredentials.BorderStyle = BorderStyle.FixedSingle;
            lvCredentials.Font = new Font("Segoe UI", 9.5f);
            lvCredentials.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvCredentials.TabIndex = 1;
            lvCredentials.TabStop = true;

            lvCredentials.Columns.Add("Sl No", 60);
            lvCredentials.Columns.Add("Service", 135);
            lvCredentials.Columns.Add("Username / Email", 165);
            lvCredentials.Columns.Add("Password", 95);
            lvCredentials.Columns.Add("Last Updated", 130);

            // Custom Dark Theme Header Painting (Matches Obsidian & Indigo container palette)
            lvCredentials.OwnerDraw = true;
            lvCredentials.DrawColumnHeader += delegate(object sender, DrawListViewColumnHeaderEventArgs e)
            {
                // Dark elevated background matching the container theme
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 23, 31)))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                }

                // Crisp bottom border and subtle column separators
                using (Pen borderPen = new Pen(ColorBorder, 1f))
                {
                    e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    if (e.ColumnIndex < lvCredentials.Columns.Count - 1)
                    {
                        e.Graphics.DrawLine(borderPen, e.Bounds.Right - 1, e.Bounds.Top + 6, e.Bounds.Right - 1, e.Bounds.Bottom - 7);
                    }
                }

                // Header text styled with muted typography matching field labels
                Rectangle textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height);
                using (Font headerFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        e.Header.Text,
                        headerFont,
                        textRect,
                        ColorTextMuted,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis
                    );
                }
            };

            lvCredentials.DrawItem += delegate(object sender, DrawListViewItemEventArgs e)
            {
                // In Details mode, DrawSubItem handles all cell rendering
            };

            lvCredentials.DrawSubItem += delegate(object sender, DrawListViewSubItemEventArgs e)
            {
                bool isSelected = e.Item.Selected;
                Color bgColor = isSelected ? Color.FromArgb(38, 48, 78) : ColorBgCard;
                Color textColor = isSelected ? Color.White : ColorTextPrimary;

                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                }

                // If selected and first column, draw a vibrant left accent pill indicator
                if (isSelected && e.ColumnIndex == 0)
                {
                    using (SolidBrush accentBrush = new SolidBrush(ColorPrimary))
                    {
                        e.Graphics.FillRectangle(accentBrush, e.Bounds.Left, e.Bounds.Top + 2, 3, e.Bounds.Height - 4);
                    }
                }

                // Subtle bottom border separator between rows
                using (Pen linePen = new Pen(Color.FromArgb(24, 30, 46), 1f))
                {
                    e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }

                Rectangle textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height);
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, textBounds, textColor, flags);
            };

            lvCredentials.SelectedIndexChanged += new EventHandler(LvCredentials_SelectedIndexChanged);
            lvCredentials.DoubleClick += new EventHandler(BtnCopyPassword_Click);
            lvCredentials.Resize += delegate { AdjustListViewColumns(); };
            lvCredentials.ColumnWidthChanged += delegate(object s, ColumnWidthChangedEventArgs e)
            {
                if (e.ColumnIndex != lvCredentials.Columns.Count - 1)
                {
                    AdjustListViewColumns();
                }
            };
            lvCredentials.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                ListViewHitTestInfo hit = lvCredentials.HitTest(e.Location);
                if (hit.Item == null)
                {
                    lvCredentials.SelectedItems.Clear();
                }
            };
            lvCredentials.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    lvCredentials.SelectedItems.Clear();
                    e.Handled = true;
                }
            };
            panelLeft.Controls.Add(lvCredentials);
            lvCredentials.BringToFront();

            this.Shown += delegate
            {
                AdjustListViewColumns();
                this.ActiveControl = txtSearch;
                txtSearch.Focus();
                txtSearch.SelectAll();
            };

            // ------------------------------------------
            // KEYBOARD SHORTCUTS & TOOLTIPS
            // ------------------------------------------
            ToolTip toolTip = new ToolTip();
            toolTip.BackColor = ColorBgCard;
            toolTip.ForeColor = ColorTextPrimary;
            toolTip.SetToolTip(txtSearch, "Search credentials (Ctrl+F, Down to navigate)");
            toolTip.SetToolTip(cmbSearchColumn, "Select column to search against (Default: Sl No)");
            toolTip.SetToolTip(lvCredentials, "Navigate with Up/Down, Enter to edit, Double-click/Ctrl+C to copy password, Delete to remove");
            toolTip.SetToolTip(txtSerialNo, "Serial number order in directory (Edit to remap order)");
            toolTip.SetToolTip(btnMoveUp, "Move Up (Alt+Up)");
            toolTip.SetToolTip(btnMoveDown, "Move Down (Alt+Down)");
            toolTip.SetToolTip(btnCopyPassword, "Copy Password (Ctrl+C)");
            toolTip.SetToolTip(btnDelete, "Delete Credential (Delete)");
            toolTip.SetToolTip(btnRefresh, "Refresh List (F5)");
            toolTip.SetToolTip(btnTogglePassword, "Reveal / Hide Password (Ctrl+P / Space)");
            toolTip.SetToolTip(btnGeneratePassword, "Generate Random Password (Ctrl+G)");
            toolTip.SetToolTip(btnSave, "Save / Update Credential (Ctrl+S / Enter)");
            toolTip.SetToolTip(btnClear, "New / Clear Form (Ctrl+N / Escape)");
            toolTip.SetToolTip(btnSettings, "Security & Master Password Settings");

            // =========================================================================
            // FORM LEVEL CONTROLS ASSEMBLY (ONLY TITLEBAR IS DOCKED TO TOP!)
            // =========================================================================
            this.Padding = new Padding(1);
            this.Controls.Add(panelMain);      // Fill
            this.Controls.Add(panelStatusBar); // Bottom
            this.Controls.Add(panelTitleBar);  // Top (ONLY control with Top dock)

            // Enable edge border resizing across child panels
            EnableEdgeResizing(panelTitleBar);
            EnableEdgeResizing(panelStatusBar);
            EnableEdgeResizing(panelMain);

            InitializeTrayIcon();
            InitializeKeyboardShortcuts();

            this.ActiveControl = txtSearch;
        }

        private void AdjustListViewColumns()
        {
            if (isAdjustingColumns || lvCredentials == null || lvCredentials.Columns.Count < 5) return;
            try
            {
                isAdjustingColumns = true;
                int fixedWidth = 0;
                for (int i = 0; i < lvCredentials.Columns.Count - 1; i++)
                {
                    fixedWidth += lvCredentials.Columns[i].Width;
                }
                int remaining = lvCredentials.ClientSize.Width - fixedWidth;
                if (remaining > 80)
                {
                    lvCredentials.Columns[lvCredentials.Columns.Count - 1].Width = remaining;
                }
            }
            finally
            {
                isAdjustingColumns = false;
            }
        }

        // ==========================================
        // UI HELPERS
        // ==========================================
        private Label CreateFieldLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lbl.ForeColor = ColorTextMuted;
            lbl.Location = new Point(x, y);
            lbl.AutoSize = true;
            return lbl;
        }

        private TextBox CreateInputTextBox(int x, int y, int width)
        {
            TextBox tb = new TextBox();
            tb.Location = new Point(x, y);
            tb.Size = new Size(width, 24);
            tb.BackColor = ColorBgInput;
            tb.ForeColor = ColorTextPrimary;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = new Font("Segoe UI", 9.5f);
            return tb;
        }

        // ==========================================
        // TITLE BAR ACTIONS & WINDOW DRAGGING
        // ==========================================
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void TitleBar_DoubleClick(object sender, EventArgs e)
        {
            ToggleMaximize();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen borderPen = new Pen(ColorBorder, 1f))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
        }

        private void EnableEdgeResizing(Control ctrl)
        {
            if (ctrl == null) return;
            new EdgeResizeFilter(ctrl, this);
            foreach (Control child in ctrl.Controls)
            {
                if (child == btnMin || child == btnMax || child == btnClose) continue;
                EnableEdgeResizing(child);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyHotkeyRegistration();
        }

        private void ApplyHotkeyRegistration()
        {
            if (hotkeyManager == null) return;
            hotkeyManager.Unregister();
            if (appSettings != null && appSettings.HotkeyEnabled && this.IsHandleCreated)
            {
                string err;
                hotkeyManager.Register(this.Handle, appSettings.HotkeyModifiers, appSettings.HotkeyKey, out err);
            }
        }

        // Border Resizing via WM_NCHITTEST & IPC Message Handling
        protected override void WndProc(ref Message m)
        {
            if (hotkeyManager != null && hotkeyManager.ProcessMessage(ref m))
            {
                RestoreFromTray();
                m.Result = (IntPtr)1;
                return;
            }

            if (m.Msg == SingleInstanceController.RestoreWindowMessageId)
            {
                bool forceMaximize = (m.WParam == (IntPtr)2);
                RestoreFromTray(forceMaximize);
                m.Result = (IntPtr)1;
                return;
            }

            if (m.Msg == WM_NCHITTEST && this.WindowState == FormWindowState.Normal)
            {
                Point cursor = this.PointToClient(Cursor.Position);
                int border = 8;

                // Don't intercept top-right close/minimize buttons
                bool inTitleButtons = (cursor.X >= this.ClientSize.Width - 140 && cursor.Y <= 42);
                if (!inTitleButtons)
                {
                    bool left = cursor.X <= border;
                    bool right = cursor.X >= this.ClientSize.Width - border;
                    bool top = cursor.Y <= border;
                    bool bottom = cursor.Y >= this.ClientSize.Height - border;

                    if (top && left) { m.Result = (IntPtr)HTTOPLEFT; return; }
                    if (top && right) { m.Result = (IntPtr)HTTOPRIGHT; return; }
                    if (bottom && left) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
                    if (bottom && right) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
                    if (left) { m.Result = (IntPtr)HTLEFT; return; }
                    if (right) { m.Result = (IntPtr)HTRIGHT; return; }
                    if (top) { m.Result = (IntPtr)HTTOP; return; }
                    if (bottom) { m.Result = (IntPtr)HTBOTTOM; return; }
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && appSettings != null && appSettings.CloseToTray)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }

            if (hotkeyManager != null)
            {
                hotkeyManager.Dispose();
                hotkeyManager = null;
            }

            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            base.OnFormClosing(e);
        }

        // ==========================================
        // SYSTEM TRAY & SINGLE-INSTANCE RESTORATION
        // ==========================================
        private void InitializeTrayIcon()
        {
            try
            {
                trayMenu = new ContextMenuStrip();
                trayMenu.BackColor = ColorBgCard;
                trayMenu.ForeColor = ColorTextPrimary;
                trayMenu.RenderMode = ToolStripRenderMode.System;

                ToolStripMenuItem itemOpen = new ToolStripMenuItem("Open KeyCraft", null, delegate { RestoreFromTray(); });
                itemOpen.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                itemOpen.ForeColor = ColorTextPrimary;

                ToolStripMenuItem itemSearch = new ToolStripMenuItem("Search Credentials (Ctrl+F)", null, delegate
                {
                    RestoreFromTray();
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                });
                itemSearch.ForeColor = ColorTextPrimary;

                ToolStripMenuItem itemLock = new ToolStripMenuItem("Lock Vault", null, delegate
                {
                    LockVault();
                });
                itemLock.ForeColor = ColorTextPrimary;

                ToolStripMenuItem itemSettings = new ToolStripMenuItem("Settings...", null, delegate
                {
                    RestoreFromTray();
                    OpenSettings();
                });
                itemSettings.ForeColor = ColorTextPrimary;

                ToolStripSeparator sep = new ToolStripSeparator();

                ToolStripMenuItem itemExit = new ToolStripMenuItem("Exit", null, delegate
                {
                    QuitApplication();
                });
                itemExit.ForeColor = ColorDangerText;

                trayMenu.Items.Add(itemOpen);
                trayMenu.Items.Add(itemSearch);
                trayMenu.Items.Add(itemLock);
                trayMenu.Items.Add(itemSettings);
                trayMenu.Items.Add(sep);
                trayMenu.Items.Add(itemExit);

                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "KeyCraft Password Manager";
                notifyIcon.ContextMenuStrip = trayMenu;

                // Sleek shield tray icon matching theme
                using (Bitmap bmp = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (SolidBrush shieldBrush = new SolidBrush(ColorPrimary))
                    {
                        Point[] pts = new Point[] {
                            new Point(8, 1),
                            new Point(14, 3),
                            new Point(14, 9),
                            new Point(8, 15),
                            new Point(2, 9),
                            new Point(2, 3)
                        };
                        g.FillPolygon(shieldBrush, pts);
                    }
                    using (Pen p = new Pen(ColorTextPrimary, 1.2f))
                    {
                        g.DrawLine(p, 8, 4, 8, 11);
                        g.DrawLine(p, 5, 7, 11, 7);
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    notifyIcon.Icon = Icon.FromHandle(hIcon);
                }

                notifyIcon.DoubleClick += delegate { RestoreFromTray(); };
                notifyIcon.Click += delegate(object s, EventArgs ea)
                {
                    MouseEventArgs me = ea as MouseEventArgs;
                    if (me == null || me.Button == MouseButtons.Left)
                    {
                        RestoreFromTray();
                    }
                };
            }
            catch { }
        }

        private FormWindowState previousWindowState = FormWindowState.Normal;

        public void MinimizeToTray()
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                previousWindowState = this.WindowState;
            }
            this.Hide();
            this.ShowInTaskbar = false;

            if (notifyIcon != null)
            {
                notifyIcon.Visible = true;
                if (!hasShownTrayTip)
                {
                    notifyIcon.ShowBalloonTip(
                        2500,
                        "KeyCraft Running in Background",
                        "KeyCraft is minimized to the system tray. Press the shortcut or double-click to reopen.",
                        ToolTipIcon.Info
                    );
                    hasShownTrayTip = true;
                }
            }
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

            this.Show();
            this.ShowInTaskbar = true;

            if (forceMaximize || previousWindowState == FormWindowState.Maximized)
            {
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }

            if (btnMax != null)
            {
                btnMax.IsMaximized = (this.WindowState == FormWindowState.Maximized);
                btnMax.Invalidate();
            }

            this.BringToFront();
            this.Activate();
            NativeMethods.SetForegroundWindow(this.Handle);

            txtSearch.Focus();
            txtSearch.SelectAll();
        }

        private void QuitApplication()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            Application.Exit();
        }

        // ==========================================
        // KEYBOARD NAVIGATION & ACCELERATORS
        // ==========================================
        private void InitializeKeyboardShortcuts()
        {
            shortcutManager = new KeyboardShortcutManager();

            // 1. Double Escape -> Minimize to System Tray
            shortcutManager.DoubleEscapeTriggered = delegate
            {
                MinimizeToTray();
            };

            // 2. Single Escape -> Form field / search cleanup
            shortcutManager.SingleEscapeTriggered = delegate
            {
                HandleSingleEscape();
            };

            // 3. Alt+Up / Alt+Down Reordering
            shortcutManager.MoveUpTriggered = delegate { btnMoveUp.PerformClick(); };
            shortcutManager.MoveDownTriggered = delegate { btnMoveDown.PerformClick(); };

            // 4. Ctrl+C (Copy Password)
            shortcutManager.CopyPasswordTriggered = delegate { btnCopyPassword.PerformClick(); };

            // 5. Ctrl+F Focus Search
            shortcutManager.FocusSearchTriggered = delegate
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
            };

            // 6. Ctrl+S Save
            shortcutManager.SaveCredentialTriggered = delegate { btnSave.PerformClick(); };

            // 7. Ctrl+N Clear/New
            shortcutManager.ClearFormTriggered = delegate
            {
                ClearEditor();
                txtService.Focus();
            };

            // 8. Ctrl+P / Space Toggle Password
            shortcutManager.TogglePasswordTriggered = delegate { btnTogglePassword.PerformClick(); };

            // 9. Ctrl+G Generate Password
            shortcutManager.GeneratePasswordTriggered = delegate { btnGeneratePassword.PerformClick(); };

            // 10. Ctrl+, Settings
            shortcutManager.OpenSettingsTriggered = delegate { OpenSettings(); };

            // 11. F5 Refresh
            shortcutManager.RefreshTriggered = delegate { btnRefresh.PerformClick(); };

            // 12. Delete
            shortcutManager.DeleteTriggered = delegate { btnDelete.PerformClick(); };

            // 13. Enter Key: Jump to Edit Selected Item
            shortcutManager.EnterEditTriggered = delegate
            {
                if (lvCredentials.SelectedItems.Count == 0 && lvCredentials.Items.Count > 0)
                {
                    lvCredentials.Items[0].Selected = true;
                }
                txtService.Focus();
                txtService.SelectAll();
            };

            // 14. Down Arrow: Search Box -> List Navigation
            shortcutManager.DownToNavigateListTriggered = delegate
            {
                lvCredentials.Focus();
                if (lvCredentials.SelectedItems.Count == 0 && lvCredentials.Items.Count > 0)
                {
                    lvCredentials.Items[0].Selected = true;
                }
            };

            // 15. Up Arrow: Top of List -> Search Box Focus
            shortcutManager.UpToNavigateSearchTriggered = delegate
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
            };
        }

        private void HandleSingleEscape()
        {
            if (txtSearch.Focused)
            {
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    txtSearch.Text = string.Empty;
                }
                else if (lvCredentials.Items.Count > 0)
                {
                    lvCredentials.Focus();
                }
            }
            else if (lvCredentials.Focused)
            {
                if (lvCredentials.SelectedItems.Count > 0)
                {
                    lvCredentials.SelectedItems.Clear();
                }
            }
            else if (txtService.Focused || txtUsername.Focused || txtPassword.Focused || txtSerialNo.Focused)
            {
                ClearEditor();
                lvCredentials.Focus();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool handled = false;
            if (shortcutManager != null)
            {
                bool isTopSelected = (lvCredentials.SelectedIndices.Count > 0 && lvCredentials.SelectedIndices[0] == 0);
                handled = shortcutManager.HandleCmdKey(
                    keyData,
                    this.ActiveControl,
                    txtSearch.Focused,
                    !string.IsNullOrEmpty(txtSearch.Text),
                    txtSearch.SelectionLength,
                    lvCredentials.Items.Count > 0,
                    lvCredentials.SelectedItems.Count > 0,
                    isTopSelected
                );
            }

            if (handled) return true;

            // KeePass Global Vault Shortcuts
            if (keyData == (Keys.Control | Keys.O))
            {
                OpenVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.N))
            {
                NewVault();
                return true;
            }
            if (keyData == (Keys.Control | Keys.L))
            {
                LockVault();
                return true;
            }

            // Sequential Enter key traversal between editor inputs
            if (keyData == Keys.Enter)
            {
                if (txtSerialNo.Focused)
                {
                    txtService.Focus();
                    txtService.SelectAll();
                    return true;
                }
                if (txtService.Focused)
                {
                    txtUsername.Focus();
                    txtUsername.SelectAll();
                    return true;
                }
                if (txtUsername.Focused)
                {
                    txtPassword.Focus();
                    txtPassword.SelectAll();
                    return true;
                }
                if (txtPassword.Focused)
                {
                    btnSave.PerformClick();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ==========================================
        // CREDENTIAL DATA & EVENTS
        // ==========================================
        private void LoadCredentials()
        {
            try
            {
                cachedList = service.LoadAll();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load credentials: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            lvCredentials.Items.Clear();
            string searchColumn = (cmbSearchColumn != null && cmbSearchColumn.SelectedItem != null)
                ? cmbSearchColumn.SelectedItem.ToString()
                : "Sl No";

            List<Credential> filtered = service.Filter(cachedList, txtSearch.Text, searchColumn);

            foreach (Credential c in filtered)
            {
                ListViewItem item = new ListViewItem(c.SerialNo.ToString());
                item.SubItems.Add(c.Service);
                item.SubItems.Add(string.IsNullOrEmpty(c.Username) ? "-" : c.Username);
                item.SubItems.Add("••••••••");
                item.SubItems.Add(c.LastUpdated.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = c;
                lvCredentials.Items.Add(item);
            }

            // Always auto-select the first item in the list by default
            if (lvCredentials.Items.Count > 0)
            {
                lvCredentials.Items[0].Selected = true;
                lvCredentials.Items[0].Focused = true;
            }

            lblStatusCount.Text = string.Format("Total: {0} ({1} shown)", cachedList != null ? cachedList.Count : 0, filtered.Count);
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void LvCredentials_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvCredentials.SelectedItems.Count > 0)
            {
                selectedCredential = lvCredentials.SelectedItems[0].Tag as Credential;
                if (selectedCredential != null)
                {
                    lblEditorHeader.Text = "Edit Credential";
                    lblEditorHeader.ForeColor = ColorWarningBg;
                    txtSerialNo.Text = selectedCredential.SerialNo.ToString();
                    txtService.Text = selectedCredential.Service;
                    txtUsername.Text = selectedCredential.Username;
                    txtPassword.Text = selectedCredential.Password;
                    btnSave.Text = "Update Credential";
                    btnSave.NormalBg = ColorWarningBg;
                    btnSave.HoverBg = ColorWarningHover;
                    btnSave.BorderColor = ColorWarningHover;
                    btnSave.NormalFg = Color.White;
                }
            }
            else
            {
                RevertToNewCredential();
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearEditor();
        }

        private void ClearEditor()
        {
            if (lvCredentials.SelectedItems.Count > 0)
            {
                lvCredentials.SelectedItems.Clear();
            }
            else
            {
                RevertToNewCredential();
            }
        }

        private void RevertToNewCredential()
        {
            selectedCredential = null;
            lblEditorHeader.Text = "New Credential";
            lblEditorHeader.ForeColor = ColorTextPrimary;
            txtSerialNo.Text = (cachedList != null ? (cachedList.Count + 1) : 1).ToString();
            txtService.Text = string.Empty;
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
            btnSave.Text = "Save Credential";
            btnSave.NormalBg = ColorPrimary;
            btnSave.HoverBg = ColorPrimaryHover;
            btnSave.BorderColor = ColorPrimaryHover;
            btnSave.NormalFg = Color.White;
            lblStrengthStatus.Text = "Strength: None";
            lblStrengthStatus.ForeColor = ColorTextMuted;
            isPasswordRevealed = false;
            txtPassword.UseSystemPasswordChar = true;
            btnTogglePassword.IconType = ButtonIcon.Eye;
            btnTogglePassword.Invalidate();
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (selectedCredential != null && selectedCredential.SerialNo > 1)
            {
                string id = selectedCredential.Id;
                int targetSerial = selectedCredential.SerialNo - 1;
                service.ReorderCredential(id, targetSerial);
                LoadCredentials();
                SelectCredentialById(id);
                ShowToast(string.Format("▲ Moved to Sl No {0}", targetSerial));
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (selectedCredential != null && cachedList != null && selectedCredential.SerialNo < cachedList.Count)
            {
                string id = selectedCredential.Id;
                int targetSerial = selectedCredential.SerialNo + 1;
                service.ReorderCredential(id, targetSerial);
                LoadCredentials();
                SelectCredentialById(id);
                ShowToast(string.Format("▼ Moved to Sl No {0}", targetSerial));
            }
        }

        private void SelectCredentialById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (ListViewItem item in lvCredentials.Items)
            {
                Credential c = item.Tag as Credential;
                if (c != null && c.Id == id)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string serviceName = txtService.Text;
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            int targetSerial = 0;
            if (!string.IsNullOrEmpty(txtSerialNo.Text))
            {
                int.TryParse(txtSerialNo.Text.Trim(), out targetSerial);
            }

            string error;
            if (!service.Validate(serviceName, username, password, out error))
            {
                MessageBox.Show(this, error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (selectedCredential == null)
                {
                    service.AddCredential(serviceName, username, password, targetSerial);
                    ShowToast("✓ New credential saved!");
                }
                else
                {
                    service.UpdateCredential(selectedCredential.Id, serviceName, username, password, targetSerial);
                    ShowToast("✓ Credential updated!");
                }

                LoadCredentials();
                ClearEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Save failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lvCredentials.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select a credential from the list to delete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Credential cred = lvCredentials.SelectedItems[0].Tag as Credential;
            if (cred == null) return;

            DialogResult res = MessageBox.Show(
                this,
                string.Format("Are you sure you want to delete the credential for '{0}'?", cred.Service),
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (res == DialogResult.Yes)
            {
                service.DeleteCredential(cred.Id);
                ShowToast("🗑️ Credential deleted.");
                LoadCredentials();
                ClearEditor();
            }
        }

        private void BtnCopyPassword_Click(object sender, EventArgs e)
        {
            Credential target = null;
            if (lvCredentials.SelectedItems.Count > 0)
            {
                target = lvCredentials.SelectedItems[0].Tag as Credential;
            }
            else if (selectedCredential != null)
            {
                target = selectedCredential;
            }
            else if (lvCredentials.Items.Count > 0 && lvCredentials.Items[0].Tag != null)
            {
                target = lvCredentials.Items[0].Tag as Credential;
            }
            else if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                SafeSetClipboardText(txtPassword.Text);
                ShowToast("✓ Password copied to clipboard!");
                return;
            }

            if (target != null && !string.IsNullOrEmpty(target.Password))
            {
                SafeSetClipboardText(target.Password);
                ShowToast(string.Format("✓ Password for '{0}' copied!", target.Service));
            }
            else
            {
                MessageBox.Show(this, "Please select a credential to copy its password.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SafeSetClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetDataObject(text, true, 5, 50);
                    return;
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        private void BtnTogglePassword_Click(object sender, EventArgs e)
        {
            isPasswordRevealed = !isPasswordRevealed;
            txtPassword.UseSystemPasswordChar = !isPasswordRevealed;
            btnTogglePassword.IconType = isPasswordRevealed ? ButtonIcon.EyeOff : ButtonIcon.Eye;
            btnTogglePassword.Invalidate();
        }

        private void BtnGeneratePassword_Click(object sender, EventArgs e)
        {
            string generated = service.GeneratePassword(16);
            txtPassword.Text = generated;
            if (!isPasswordRevealed)
            {
                isPasswordRevealed = true;
                txtPassword.UseSystemPasswordChar = false;
            }
            ShowToast("⚡ Generated secure random password!");
        }

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            string label;
            int score = service.EvaluateStrength(txtPassword.Text, out label);

            Color c = ColorTextMuted;
            if (score == 1) c = ColorDangerText;
            else if (score == 2) c = ColorWarningHover;
            else if (score >= 3) c = ColorSuccess;

            lblStrengthStatus.Text = "Strength: " + label;
            lblStrengthStatus.ForeColor = c;
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadCredentials();
            ShowToast("🔄 List refreshed.");
        }

        private void ShowToast(string message)
        {
            lblToast.Text = message;
            lblToast.Visible = true;
            timerToast.Stop();
            timerToast.Start();
        }

        private void TimerToast_Tick(object sender, EventArgs e)
        {
            lblToast.Visible = false;
            timerToast.Stop();
        }

        private void OpenSettings()
        {
            using (SettingsForm settingsForm = new SettingsForm(service, appSettings))
            {
                settingsForm.SettingsSaved += delegate
                {
                    ApplyHotkeyRegistration();
                };
                settingsForm.VaultLockRequested += delegate
                {
                    LockVaultAndPrompt();
                };
                settingsForm.ShowDialog(this);
            }
        }

        private void LockVaultAndPrompt()
        {
            service.LockVault();
            this.Hide();
            string repoPath = service.GetVaultFilePath();
            using (MasterPasswordForm unlockForm = new MasterPasswordForm(MasterPasswordMode.Unlock, repoPath))
            {
                if (unlockForm.ShowDialog() == DialogResult.OK)
                {
                    if (!string.Equals(unlockForm.SelectedVaultPath, repoPath, StringComparison.OrdinalIgnoreCase))
                    {
                        SwitchToVaultFile(unlockForm.SelectedVaultPath, true);
                    }
                    this.Show();
                    LoadCredentials();
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        // ==========================================
        // KEEPASS MULTI-VAULT OPERATIONS
        // ==========================================

        public void UpdateTitleAndVaultDisplay()
        {
            string vaultPath = service.GetVaultFilePath();
            string fileName = System.IO.Path.GetFileName(vaultPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "vault.kcrypt";

            this.Text = string.Format("KeyCraft — [{0}]", fileName);
            if (lblAppTitle != null)
            {
                lblAppTitle.Text = string.Format("KeyCraft  —  [{0}]", fileName);
            }
            if (lblVaultBadge != null)
            {
                lblVaultBadge.Text = "📁 " + fileName;
                ToolTip tt = new ToolTip();
                tt.SetToolTip(lblVaultBadge, "Active Vault: " + vaultPath + "\nClick to open another database");
            }
            if (lblStatusFile != null)
            {
                lblStatusFile.Text = "📁 " + vaultPath;
            }
        }

        public void OpenVault()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Open KeyCraft Vault Database";
                ofd.Filter = "KeyCraft Vault (*.kcrypt;*.kdb;*.txt)|*.kcrypt;*.kdb;*.txt|All Files (*.*)|*.*";
                ofd.CheckFileExists = true;
                string currentDir = System.IO.Path.GetDirectoryName(service.GetVaultFilePath());
                if (!string.IsNullOrEmpty(currentDir) && System.IO.Directory.Exists(currentDir))
                {
                    ofd.InitialDirectory = currentDir;
                }

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    SwitchToVaultFile(ofd.FileName);
                }
            }
        }

        public void NewVault()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Create New KeyCraft Vault Database";
                sfd.Filter = "KeyCraft Encrypted Vault (*.kcrypt)|*.kcrypt|All Files (*.*)|*.*";
                sfd.DefaultExt = "kcrypt";
                sfd.AddExtension = true;
                sfd.FileName = "vault.kcrypt";
                string currentDir = System.IO.Path.GetDirectoryName(service.GetVaultFilePath());
                if (!string.IsNullOrEmpty(currentDir) && System.IO.Directory.Exists(currentDir))
                {
                    sfd.InitialDirectory = currentDir;
                }

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    CreateAndSwitchVault(sfd.FileName);
                }
            }
        }

        public void LockVault()
        {
            LockVaultAndPrompt();
        }

        public void SwitchToVaultFile(string newPath, bool alreadyUnlocked = false)
        {
            if (string.IsNullOrEmpty(newPath)) return;
            try
            {
                string fullPath = System.IO.Path.GetFullPath(newPath);
                if (!alreadyUnlocked)
                {
                    bool isEncrypted = VaultSecurity.IsVaultEncrypted(fullPath);
                    MasterPasswordMode m = isEncrypted ? MasterPasswordMode.Unlock : MasterPasswordMode.Create;
                    using (MasterPasswordForm form = new MasterPasswordForm(m, fullPath))
                    {
                        if (form.ShowDialog(this) != DialogResult.OK)
                        {
                            return; // Cancelled
                        }
                        fullPath = form.SelectedVaultPath;
                    }
                }

                service.SwitchDatabase(fullPath);
                appSettings.AddRecentVault(fullPath);
                appSettings.Save();

                UpdateTitleAndVaultDisplay();
                LoadCredentials();
                ClearEditor();
                ShowToast("📁 Active Vault: " + System.IO.Path.GetFileName(fullPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to switch vault: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CreateAndSwitchVault(string newPath)
        {
            if (string.IsNullOrEmpty(newPath)) return;
            try
            {
                string fullPath = System.IO.Path.GetFullPath(newPath);
                using (MasterPasswordForm createForm = new MasterPasswordForm(MasterPasswordMode.Create, fullPath))
                {
                    if (createForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return; // Cancelled
                    }
                    fullPath = createForm.SelectedVaultPath;
                }

                service.SwitchDatabase(fullPath);
                appSettings.AddRecentVault(fullPath);
                appSettings.Save();

                UpdateTitleAndVaultDisplay();
                LoadCredentials();
                ClearEditor();
                ShowToast("✓ New vault created & active!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to create vault: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
