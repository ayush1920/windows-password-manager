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
        Shield
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
        private bool isSearchFocused = false;
        private ListView lvCredentials;
        private bool isAdjustingColumns = false;
        private ModernButton btnCopyPassword;
        private ModernButton btnDelete;
        private ModernButton btnRefresh;

        // Right Panel (Editor)
        private Label lblEditorHeader;
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

            InitializeComponent();
            LoadCredentials();
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
            btnMin.Click += delegate { this.WindowState = FormWindowState.Minimized; };
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
            lblStatusFile.Text = "● Active Storage: " + System.IO.Path.GetFileName(service.StorageFilePath);
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

            // Field: Service
            Label lblService = CreateFieldLabel("SERVICE / WEBSITE *", 18, 48);
            panelRight.Controls.Add(lblService);

            txtService = CreateInputTextBox(18, 70, 310);
            txtService.TabIndex = 5;
            txtService.TabStop = true;
            panelRight.Controls.Add(txtService);

            // Field: Username
            Label lblUser = CreateFieldLabel("USERNAME / EMAIL", 18, 112);
            panelRight.Controls.Add(lblUser);

            txtUsername = CreateInputTextBox(18, 134, 310);
            txtUsername.TabIndex = 6;
            txtUsername.TabStop = true;
            panelRight.Controls.Add(txtUsername);

            // Field: Password
            Label lblPwd = CreateFieldLabel("PASSWORD *", 18, 176);
            panelRight.Controls.Add(lblPwd);

            txtPassword = CreateInputTextBox(18, 198, 226);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Font = fontMono;
            txtPassword.TabIndex = 7;
            txtPassword.TabStop = true;
            txtPassword.TextChanged += new EventHandler(TxtPassword_TextChanged);
            panelRight.Controls.Add(txtPassword);

            // Toggle Password Button (5.0px rounded with Lucide Eye Icon)
            btnTogglePassword = new ModernButton();
            btnTogglePassword.Location = new Point(250, 198);
            btnTogglePassword.Size = new Size(36, 26);
            btnTogglePassword.CornerRadius = 5.0f;
            btnTogglePassword.IconType = ButtonIcon.Eye;
            btnTogglePassword.NormalBg = ColorSecondary;
            btnTogglePassword.HoverBg = ColorSecondaryHover;
            btnTogglePassword.BorderColor = ColorSecondaryBorder;
            btnTogglePassword.NormalFg = ColorTextMuted;
            btnTogglePassword.TabIndex = 8;
            btnTogglePassword.TabStop = true;
            btnTogglePassword.Click += new EventHandler(BtnTogglePassword_Click);
            panelRight.Controls.Add(btnTogglePassword);

            // Generate Random Password Button (5.0px rounded with Lucide Key Icon)
            btnGeneratePassword = new ModernButton();
            btnGeneratePassword.Location = new Point(292, 198);
            btnGeneratePassword.Size = new Size(36, 26);
            btnGeneratePassword.CornerRadius = 5.0f;
            btnGeneratePassword.IconType = ButtonIcon.Key;
            btnGeneratePassword.NormalBg = ColorPrimary;
            btnGeneratePassword.HoverBg = ColorPrimaryHover;
            btnGeneratePassword.BorderColor = ColorPrimary;
            btnGeneratePassword.NormalFg = Color.White;
            btnGeneratePassword.TabIndex = 9;
            btnGeneratePassword.TabStop = true;
            btnGeneratePassword.Click += new EventHandler(BtnGeneratePassword_Click);
            panelRight.Controls.Add(btnGeneratePassword);

            // Password Strength Status
            lblStrengthStatus = new Label();
            lblStrengthStatus.Text = "Strength: None";
            lblStrengthStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblStrengthStatus.ForeColor = ColorTextMuted;
            lblStrengthStatus.Location = new Point(18, 230);
            lblStrengthStatus.AutoSize = true;
            panelRight.Controls.Add(lblStrengthStatus);

            // Save Credential Button (5.0px rounded with Lucide Save Icon)
            btnSave = new ModernButton();
            btnSave.Text = "Save Credential";
            btnSave.Location = new Point(18, 264);
            btnSave.Size = new Size(310, 38);
            btnSave.CornerRadius = 5.0f;
            btnSave.IconType = ButtonIcon.Save;
            btnSave.NormalBg = ColorPrimary;
            btnSave.HoverBg = ColorPrimaryHover;
            btnSave.PressedBg = ColorPrimaryPressed;
            btnSave.BorderColor = ColorPrimaryHover;
            btnSave.NormalFg = Color.White;
            btnSave.Font = fontBold;
            btnSave.TabIndex = 10;
            btnSave.TabStop = true;
            btnSave.Click += new EventHandler(BtnSave_Click);
            panelRight.Controls.Add(btnSave);

            // Clear Form Button (5.0px rounded with Lucide Plus Icon)
            btnClear = new ModernButton();
            btnClear.Text = "New / Clear Form";
            btnClear.Location = new Point(18, 312);
            btnClear.Size = new Size(310, 32);
            btnClear.CornerRadius = 5.0f;
            btnClear.IconType = ButtonIcon.Plus;
            btnClear.NormalBg = ColorSecondary;
            btnClear.HoverBg = ColorSecondaryHover;
            btnClear.PressedBg = Color.FromArgb(26, 29, 40);
            btnClear.BorderColor = ColorSecondaryBorder;
            btnClear.NormalFg = ColorTextMuted;
            btnClear.Font = fontRegular;
            btnClear.TabIndex = 11;
            btnClear.TabStop = true;
            btnClear.Click += new EventHandler(BtnClear_Click);
            panelRight.Controls.Add(btnClear);

            // Toast feedback label
            lblToast = new Label();
            lblToast.Text = "✓ Saved successfully!";
            lblToast.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblToast.ForeColor = ColorSuccess;
            lblToast.Location = new Point(18, 356);
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

            // Search Bar Container (Integrated input bar: panel -> icon -> borderless textbox)
            panelSearch = new Panel();
            panelSearch.Dock = DockStyle.Top;
            panelSearch.Height = 32;
            panelSearch.BackColor = ColorBgInput;
            panelSearch.Padding = new Padding(2, 2, 2, 2);
            panelSearch.Cursor = Cursors.IBeam;
            panelSearch.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen borderPen = new Pen(isSearchFocused ? ColorPrimary : ColorBorder, 1f))
                {
                    pe.Graphics.DrawRectangle(borderPen, 0, 0, panelSearch.Width - 1, panelSearch.Height - 1);
                }
            };

            // Lucide Search Icon (16x16 inside search bar)
            // Outer panel padding = 2px all across -> icon X = 2
            Panel panelSearchIcon = new Panel();
            panelSearchIcon.Size = new Size(16, 16);
            panelSearchIcon.Location = new Point(2, (panelSearch.Height - 16) / 2);
            panelSearchIcon.BackColor = Color.Transparent;
            panelSearchIcon.Cursor = Cursors.IBeam;
            panelSearchIcon.Paint += delegate(object s, PaintEventArgs pe)
            {
                Bitmap searchBmp = IconResources.GetIcon("search");
                if (searchBmp != null)
                {
                    IconHelper.DrawTintedIcon(pe.Graphics, searchBmp, new Rectangle(0, 0, 16, 16), isSearchFocused ? ColorPrimaryHover : ColorTextMuted);
                }
            };
            panelSearch.Controls.Add(panelSearchIcon);

            // Input Box without borders:
            // Outer panel left padding: 2px
            // Search icon: 16px width (ends at 2 + 16 = 18px)
            // Space between search icon and input box: 5px (reaches 23px)
            // Input box padding: 4px on left (starts at 23 + 4 = 27px)
            // Top padding: 2px outer + 4px inner centering adjustment (Y = 6px) to align Segoe UI 9.5pt text with 16x16 icon
            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = ColorBgInput;
            txtSearch.ForeColor = ColorTextPrimary;
            txtSearch.Font = new Font("Segoe UI", 9.5f);
            txtSearch.TabIndex = 0;
            txtSearch.TabStop = true;
            
            int inputLeft = 2 + 16 + 5 + 4; // 27px
            int inputTop = 6;               // Centered with icon
            txtSearch.Location = new Point(inputLeft, inputTop);
            txtSearch.Size = new Size(Math.Max(50, 560 - inputLeft - 6), 18);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            panelSearch.Resize += delegate
            {
                txtSearch.Width = Math.Max(50, panelSearch.ClientSize.Width - inputLeft - 6);
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

            panelLeft.Controls.Add(panelLeftBottom);

            // ListView with Obsidian Card Styling
            lvCredentials = new ListView();
            lvCredentials.Dock = DockStyle.Fill;
            lvCredentials.View = View.Details;
            lvCredentials.FullRowSelect = true;
            lvCredentials.MultiSelect = false;
            lvCredentials.BackColor = ColorBgCard;
            lvCredentials.ForeColor = ColorTextPrimary;
            lvCredentials.BorderStyle = BorderStyle.FixedSingle;
            lvCredentials.Font = new Font("Segoe UI", 9.5f);
            lvCredentials.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvCredentials.TabIndex = 1;
            lvCredentials.TabStop = true;

            lvCredentials.Columns.Add("Service", 140);
            lvCredentials.Columns.Add("Username / Email", 175);
            lvCredentials.Columns.Add("Password", 100);
            lvCredentials.Columns.Add("Last Updated", 140);

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
                e.DrawDefault = true;
            };

            lvCredentials.DrawSubItem += delegate(object sender, DrawListViewSubItemEventArgs e)
            {
                e.DrawDefault = true;
            };

            lvCredentials.SelectedIndexChanged += new EventHandler(LvCredentials_SelectedIndexChanged);
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

            this.Shown += delegate { AdjustListViewColumns(); };

            // ------------------------------------------
            // KEYBOARD SHORTCUTS & TOOLTIPS
            // ------------------------------------------
            ToolTip toolTip = new ToolTip();
            toolTip.BackColor = ColorBgCard;
            toolTip.ForeColor = ColorTextPrimary;
            toolTip.SetToolTip(txtSearch, "Search credentials (Ctrl+F, Down to navigate)");
            toolTip.SetToolTip(lvCredentials, "Navigate with Up/Down, Enter to edit, Delete to remove, Ctrl+C to copy password");
            toolTip.SetToolTip(btnCopyPassword, "Copy Password (Ctrl+C)");
            toolTip.SetToolTip(btnDelete, "Delete Credential (Delete)");
            toolTip.SetToolTip(btnRefresh, "Refresh List (F5)");
            toolTip.SetToolTip(btnTogglePassword, "Reveal / Hide Password (Ctrl+P / Space)");
            toolTip.SetToolTip(btnGeneratePassword, "Generate Random Password (Ctrl+G)");
            toolTip.SetToolTip(btnSave, "Save / Update Credential (Ctrl+S / Enter)");
            toolTip.SetToolTip(btnClear, "New / Clear Form (Ctrl+N / Escape)");

            // =========================================================================
            // FORM LEVEL CONTROLS ASSEMBLY (ONLY TITLEBAR IS DOCKED TO TOP!)
            // =========================================================================
            this.Controls.Add(panelMain);      // Fill
            this.Controls.Add(panelStatusBar); // Bottom
            this.Controls.Add(panelTitleBar);  // Top (ONLY control with Top dock)
        }

        private void AdjustListViewColumns()
        {
            if (isAdjustingColumns || lvCredentials == null || lvCredentials.Columns.Count < 4) return;
            try
            {
                isAdjustingColumns = true;
                int fixedWidth = 0;
                for (int i = 0; i < lvCredentials.Columns.Count - 1; i++)
                {
                    fixedWidth += lvCredentials.Columns[i].Width;
                }
                int remaining = lvCredentials.ClientSize.Width - fixedWidth;
                if (remaining > 100)
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

        // Border Resizing via WM_NCHITTEST
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                Point cursor = this.PointToClient(Cursor.Position);
                int border = 8;

                bool left = cursor.X <= border;
                bool right = cursor.X >= this.ClientSize.Width - border;
                bool top = cursor.Y <= border;
                bool bottom = cursor.Y >= this.ClientSize.Height - border;

                if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left) m.Result = (IntPtr)HTLEFT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (top) m.Result = (IntPtr)HTTOP;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
            }
        }

        // ==========================================
        // KEYBOARD NAVIGATION & ACCELERATORS
        // ==========================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. Global Keyboard Accelerators
            if (keyData == (Keys.Control | Keys.F))
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                return true;
            }
            if (keyData == (Keys.Control | Keys.N))
            {
                ClearEditor();
                txtService.Focus();
                return true;
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                btnSave.PerformClick();
                return true;
            }
            if (keyData == (Keys.Control | Keys.G))
            {
                btnGeneratePassword.PerformClick();
                return true;
            }
            if (keyData == (Keys.Control | Keys.P) || keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                btnTogglePassword.PerformClick();
                return true;
            }
            if (keyData == Keys.F5)
            {
                btnRefresh.PerformClick();
                return true;
            }

            // 2. Escape Key Navigation
            if (keyData == Keys.Escape)
            {
                if (txtSearch.Focused)
                {
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        txtSearch.Text = string.Empty;
                        return true;
                    }
                    else if (lvCredentials.Items.Count > 0)
                    {
                        lvCredentials.Focus();
                        return true;
                    }
                }
                else if (lvCredentials.Focused)
                {
                    if (lvCredentials.SelectedItems.Count > 0)
                    {
                        lvCredentials.SelectedItems.Clear();
                        return true;
                    }
                }
                else if (txtService.Focused || txtUsername.Focused || txtPassword.Focused)
                {
                    ClearEditor();
                    lvCredentials.Focus();
                    return true;
                }
            }

            // 3. Enter Key Navigation
            if (keyData == Keys.Enter)
            {
                if (txtSearch.Focused)
                {
                    if (lvCredentials.Items.Count > 0)
                    {
                        lvCredentials.Focus();
                        if (lvCredentials.SelectedItems.Count == 0)
                        {
                            lvCredentials.Items[0].Selected = true;
                        }
                        return true;
                    }
                }
                else if (lvCredentials.Focused)
                {
                    if (lvCredentials.SelectedItems.Count > 0)
                    {
                        txtService.Focus();
                        txtService.SelectAll();
                        return true;
                    }
                }
                else if (txtService.Focused)
                {
                    txtUsername.Focus();
                    txtUsername.SelectAll();
                    return true;
                }
                else if (txtUsername.Focused)
                {
                    txtPassword.Focus();
                    txtPassword.SelectAll();
                    return true;
                }
                else if (txtPassword.Focused)
                {
                    btnSave.PerformClick();
                    return true;
                }
            }

            // 4. Down Arrow: Search Box -> List
            if (keyData == Keys.Down)
            {
                if (txtSearch.Focused && lvCredentials.Items.Count > 0)
                {
                    lvCredentials.Focus();
                    if (lvCredentials.SelectedItems.Count == 0)
                    {
                        lvCredentials.Items[0].Selected = true;
                    }
                    return true;
                }
                else if (lvCredentials.Focused && lvCredentials.SelectedItems.Count == 0 && lvCredentials.Items.Count > 0)
                {
                    lvCredentials.Items[0].Selected = true;
                    return true;
                }
            }

            // 5. Up Arrow: List Item 0 -> Search Box
            if (keyData == Keys.Up && lvCredentials.Focused)
            {
                if (lvCredentials.SelectedIndices.Count > 0 && lvCredentials.SelectedIndices[0] == 0)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                    return true;
                }
            }

            // 6. Delete Key in List View
            if (keyData == Keys.Delete && lvCredentials.Focused)
            {
                if (lvCredentials.SelectedItems.Count > 0)
                {
                    btnDelete.PerformClick();
                    return true;
                }
            }

            // 7. Ctrl+C in List View (Copy Password)
            if (keyData == (Keys.Control | Keys.C) && lvCredentials.Focused)
            {
                if (lvCredentials.SelectedItems.Count > 0)
                {
                    btnCopyPassword.PerformClick();
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
            List<Credential> filtered = service.Filter(cachedList, txtSearch.Text);

            foreach (Credential c in filtered)
            {
                ListViewItem item = new ListViewItem(c.Service);
                item.SubItems.Add(string.IsNullOrEmpty(c.Username) ? "-" : c.Username);
                item.SubItems.Add("••••••••");
                item.SubItems.Add(c.LastUpdated.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = c;
                lvCredentials.Items.Add(item);
            }

            lblStatusCount.Text = string.Format("Total: {0} ({1} shown)", cachedList.Count, filtered.Count);
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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string serviceName = txtService.Text;
            string username = txtUsername.Text;
            string password = txtPassword.Text;

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
                    service.AddCredential(serviceName, username, password);
                    ShowToast("✓ New credential saved!");
                }
                else
                {
                    service.UpdateCredential(selectedCredential.Id, serviceName, username, password);
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
            else if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                Clipboard.SetText(txtPassword.Text);
                ShowToast("✓ Password copied to clipboard!");
                return;
            }

            if (target != null && !string.IsNullOrEmpty(target.Password))
            {
                Clipboard.SetText(target.Password);
                ShowToast(string.Format("✓ Password for '{0}' copied!", target.Service));
            }
            else
            {
                MessageBox.Show(this, "Please select a credential to copy its password.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
}
