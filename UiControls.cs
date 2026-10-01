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
    public static class WinColors
    {
        // Authentic Windows 11 Fluent Dark Design System (Extracted from Stitch Screens)
        public static readonly Color Desktop = Color.FromArgb(18, 18, 18);          // #121212
        public static readonly Color Window = Color.FromArgb(32, 32, 32);           // #202020
        public static readonly Color Card = Color.FromArgb(39, 39, 39);             // #272727
        public static readonly Color CardElevated = Color.FromArgb(44, 44, 44);     // #2C2C2C
        public static readonly Color TableBg = Color.FromArgb(36, 36, 36);          // #242424
        public static readonly Color InputBg = Color.FromArgb(30, 30, 30);          // #1E1E1E
        public static readonly Color Chrome = Color.FromArgb(26, 26, 26);           // #1A1A1A
        public static readonly Color StatusBar = Color.FromArgb(28, 28, 28);        // #1C1C1C

        // Borders & Dividers
        public static readonly Color Border = Color.FromArgb(56, 56, 56);           // #383838
        public static readonly Color BorderSubtle = Color.FromArgb(48, 48, 48);     // #303030
        public static readonly Color BorderLight = Color.FromArgb(62, 62, 62);      // #3E3E3E
        public static readonly Color BorderFocus = Color.FromArgb(0, 120, 212);     // #0078D4

        // Fluent Blue Accent
        public static readonly Color Accent = Color.FromArgb(0, 120, 212);          // #0078D4
        public static readonly Color AccentHover = Color.FromArgb(16, 132, 217);    // #1084D9
        public static readonly Color AccentPressed = Color.FromArgb(0, 103, 192);   // #0067C0

        // Secondary Neutral Controls
        public static readonly Color BtnSecondary = Color.FromArgb(51, 51, 51);     // #333333
        public static readonly Color BtnSecondaryHover = Color.FromArgb(61, 61, 61);// #3D3D3D
        public static readonly Color BtnSecondaryPressed = Color.FromArgb(43, 43, 43);// #2B2B2B

        // Danger / Lock Red
        public static readonly Color DangerSolid = Color.FromArgb(196, 43, 28);     // #C42B1C
        public static readonly Color DangerHover = Color.FromArgb(179, 39, 25);     // #B32719
        public static readonly Color DangerPressed = Color.FromArgb(158, 34, 22);   // #9E2216
        public static readonly Color DangerText = Color.FromArgb(224, 168, 168);    // #E0A8A8
        public static readonly Color DangerBorder = Color.FromArgb(82, 51, 51);     // #523333

        // Success / Status Green
        public static readonly Color Success = Color.FromArgb(16, 124, 65);         // #107C41
        public static readonly Color SuccessLight = Color.FromArgb(108, 203, 136);  // #6CCB88
        public static readonly Color SuccessBg = Color.FromArgb(26, 56, 38);        // #1A3826
        public static readonly Color SuccessBorder = Color.FromArgb(35, 88, 53);    // #235835

        // Warning / Weak Badges
        public static readonly Color WeakBg = Color.FromArgb(58, 29, 29);           // #3A1D1D
        public static readonly Color WeakBorder = Color.FromArgb(92, 40, 40);       // #5C2828
        public static readonly Color WeakText = Color.FromArgb(255, 125, 125);      // #FF7D7D

        public static readonly Color MediumBg = Color.FromArgb(59, 52, 31);         // #3B341F
        public static readonly Color MediumBorder = Color.FromArgb(89, 77, 41);     // #594D29
        public static readonly Color MediumText = Color.FromArgb(214, 178, 74);     // #D6B24A

        // Typography
        public static readonly Color TextWhite = Color.FromArgb(255, 255, 255);     // #FFFFFF
        public static readonly Color TextSecondary = Color.FromArgb(204, 204, 204); // #CCCCCC
        public static readonly Color TextMuted = Color.FromArgb(158, 158, 158);     // #9E9E9E
        public static readonly Color TextSubtle = Color.FromArgb(112, 112, 112);    // #707070
    }

    public enum TitleButtonType
    {
        Minimize,
        Maximize,
        Close
    }

    /// <summary>
    /// Native Windows 11 style title bar caption button (Minimize, Maximize/Restore, Close).
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
            this.Size = new Size(46, 32);
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

            Color bg = this.Parent != null ? this.Parent.BackColor : WinColors.Chrome;
            if (ButtonType == TitleButtonType.Close)
            {
                if (isPressed) bg = WinColors.DangerPressed;
                else if (isHovered) bg = WinColors.DangerSolid;
            }
            else
            {
                if (isPressed) bg = Color.FromArgb(40, 40, 40);
                else if (isHovered) bg = Color.FromArgb(51, 51, 51);
            }

            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillRectangle(b, this.ClientRectangle);
            }

            Color iconColor = (ButtonType == TitleButtonType.Close && isHovered)
                ? Color.White
                : WinColors.TextSecondary;

            int cx = this.Width / 2;
            int cy = this.Height / 2;

            using (Pen pen = new Pen(iconColor, 1f))
            {
                switch (ButtonType)
                {
                    case TitleButtonType.Minimize:
                        g.SmoothingMode = SmoothingMode.None;
                        g.DrawLine(pen, cx - 5, cy, cx + 5, cy);
                        break;

                    case TitleButtonType.Maximize:
                        g.SmoothingMode = SmoothingMode.None;
                        if (IsMaximized)
                        {
                            g.DrawRectangle(pen, cx - 3, cy - 5, 8, 8);
                            using (SolidBrush frontBg = new SolidBrush(bg))
                            {
                                g.FillRectangle(frontBg, cx - 5, cy - 3, 9, 9);
                            }
                            g.DrawRectangle(pen, cx - 5, cy - 3, 8, 8);
                        }
                        else
                        {
                            g.DrawRectangle(pen, cx - 5, cy - 5, 10, 10);
                        }
                        break;

                    case TitleButtonType.Close:
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawLine(pen, cx - 4.5f, cy - 4.5f, cx + 4.5f, cy + 4.5f);
                        g.DrawLine(pen, cx - 4.5f, cy + 4.5f, cx + 4.5f, cy - 4.5f);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Helper for drawing dynamically tinted open-source icons.
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
    /// Custom production-grade button with smooth 4px rounded corners, Windows 11 styling,
    /// dynamic icon tinting, and anti-bleed background painting.
    /// </summary>
    public class ModernButton : Button
    {
        public float CornerRadius { get; set; }
        public Color NormalBg { get; set; }
        public Color HoverBg { get; set; }
        public Color PressedBg { get; set; }
        public Color BorderColor { get; set; }
        public Color NormalFg { get; set; }
        public Color HoverFg { get; set; }
        public string IconName { get; set; }
        public int IconSize { get; set; }
        public bool ShowFocusBorder { get; set; }

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

            this.CornerRadius = 4.0f;
            this.NormalBg = WinColors.BtnSecondary;
            this.HoverBg = WinColors.BtnSecondaryHover;
            this.PressedBg = WinColors.BtnSecondaryPressed;
            this.BorderColor = WinColors.BorderLight;
            this.NormalFg = WinColors.TextWhite;
            this.HoverFg = Color.Empty;
            this.ShowFocusBorder = true;
            this.IconName = null;
            this.IconSize = 14;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
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

            Color parentBg = this.Parent != null ? this.Parent.BackColor : WinColors.Window;
            g.Clear(parentBg);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, this.Width - 1f, this.Height - 1f);
            Color currentBg = !this.Enabled ? Color.FromArgb(34, 34, 34) : (isPressed ? PressedBg : (isHovered ? HoverBg : NormalBg));
            Color currentFg = !this.Enabled 
                ? WinColors.TextSubtle 
                : ((isHovered && HoverFg != Color.Empty) ? HoverFg : NormalFg);

            using (GraphicsPath path = GetRoundedRectangle(rect, CornerRadius))
            {
                if (currentBg != Color.Transparent)
                {
                    using (SolidBrush bgBrush = new SolidBrush(currentBg))
                    {
                        g.FillPath(bgBrush, path);
                    }
                }

                Color effectiveBorder = !this.Enabled 
                    ? Color.FromArgb(48, 48, 48) 
                    : ((this.Focused && this.Enabled && ShowFocusBorder) ? WinColors.BorderFocus : BorderColor);
                if (effectiveBorder != Color.Transparent)
                {
                    using (Pen borderPen = new Pen(effectiveBorder, (this.Focused && this.Enabled && ShowFocusBorder) ? 1.5f : 1f))
                    {
                        borderPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(borderPen, path);
                    }
                }
            }

            // Draw Icon and Text with balanced margins and padding
            int iconSz = IconSize > 0 ? IconSize : 14;
            Bitmap iconBmp = !string.IsNullOrEmpty(IconName) ? IconResources.GetIcon(IconName) : null;
            
            int textWidth = 0;
            int textHeight = 0;
            if (!string.IsNullOrEmpty(this.Text))
            {
                Size sz = TextRenderer.MeasureText(g, this.Text, this.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                textWidth = sz.Width;
                textHeight = sz.Height;
            }

            int iconGap = (this.Width < 90) ? 6 : 8;
            int contentWidth = 0;
            if (iconBmp != null && textWidth > 0)
                contentWidth = iconSz + iconGap + textWidth;
            else if (iconBmp != null)
                contentWidth = iconSz;
            else
                contentWidth = textWidth;

            // Enforce equal left and right spacing with centered content
            int startX = (this.Width - contentWidth) / 2;
            if (startX < 6) startX = 6;

            int startY = (this.Height - iconSz) / 2;
            int textX = startX;

            if (iconBmp != null)
            {
                Rectangle iconRect = new Rectangle(startX, startY, iconSz, iconSz);
                IconHelper.DrawTintedIcon(g, iconBmp, iconRect, currentFg);
                textX = startX + iconSz + iconGap;
            }

            if (!string.IsNullOrEmpty(this.Text))
            {
                int textY = (this.Height - textHeight) / 2;
                int availW = Math.Max(textWidth, this.Width - textX - 2);
                Rectangle textRect = new Rectangle(textX, textY, availW, textHeight);
                TextRenderer.DrawText(g, this.Text, this.Font, textRect, currentFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
    /// Custom Windows 11 WinUI 3 styled checkbox.
    /// </summary>
    public class WinCheckbox : CheckBox
    {
        private bool isHovered = false;

        public WinCheckbox()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );
            this.Size = new Size(200, 22);
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.ForeColor = WinColors.TextSecondary;
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
            Invalidate();
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color parentBg = this.Parent != null ? this.Parent.BackColor : WinColors.Card;
            g.Clear(parentBg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isChecked = this.Checked;

            // Box
            Rectangle boxRect = new Rectangle(0, (this.Height - 16) / 2, 16, 16);
            Color boxBg = isChecked ? WinColors.Accent : (isHovered ? Color.FromArgb(46, 46, 46) : Color.FromArgb(39, 39, 39));
            Color boxBorder = isChecked ? WinColors.Accent : (isHovered ? WinColors.TextMuted : Color.FromArgb(102, 102, 102));

            using (SolidBrush b = new SolidBrush(boxBg))
            {
                g.FillRectangle(b, boxRect);
            }
            using (Pen p = new Pen(boxBorder, 1f))
            {
                g.DrawRectangle(p, boxRect);
            }

            // Checkmark
            if (isChecked)
            {
                using (Pen checkPen = new Pen(Color.White, 2f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    g.DrawLine(checkPen, boxRect.X + 3.5f, boxRect.Y + 8f, boxRect.X + 6.5f, boxRect.Y + 11.5f);
                    g.DrawLine(checkPen, boxRect.X + 6.5f, boxRect.Y + 11.5f, boxRect.X + 12.5f, boxRect.Y + 4.5f);
                }
            }

            // Text
            if (!string.IsNullOrEmpty(this.Text))
            {
                Rectangle textRect = new Rectangle(24, 0, this.Width - 24, this.Height);
                TextRenderer.DrawText(
                    g,
                    this.Text,
                    this.Font,
                    textRect,
                    isHovered ? WinColors.TextWhite : this.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            }
        }
    }

    /// <summary>
    /// Custom sleek Windows 11 Fluent dark scroll bar component.
    /// Overrides default Win32 white scrollbars with a subtle dark track and rounded thumb.
    /// </summary>
    public class DarkScrollBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private int _viewSize = 50;

        private bool isHovered = false;
        private bool isDragging = false;
        private int dragStartMouseY = 0;
        private int dragStartValue = 0;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = Math.Max(_minimum, value); Invalidate(); }
        }

        public int ViewSize
        {
            get { return _viewSize; }
            set { _viewSize = Math.Max(1, value); Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int maxScroll = Math.Max(0, _maximum - _viewSize);
                int clamped = Math.Max(_minimum, Math.Min(maxScroll, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    Invalidate();
                    if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        public DarkScrollBar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true
            );
            this.Width = 8;
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Default;
        }

        private Rectangle GetThumbRectangle()
        {
            int trackH = this.Height;
            int maxScroll = Math.Max(1, _maximum - _viewSize);
            if (_maximum <= _viewSize || trackH <= 20)
            {
                return Rectangle.Empty;
            }

            int thumbH = Math.Max(24, (int)((float)_viewSize / _maximum * trackH));
            int availableTrack = trackH - thumbH;
            int thumbY = (int)((float)_value / maxScroll * availableTrack);
            return new Rectangle(1, thumbY, this.Width - 2, thumbH);
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
            if (!isDragging)
            {
                isHovered = false;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Rectangle thumb = GetThumbRectangle();
                if (thumb != Rectangle.Empty && thumb.Contains(e.Location))
                {
                    isDragging = true;
                    dragStartMouseY = e.Y;
                    dragStartValue = _value;
                }
                else if (thumb != Rectangle.Empty)
                {
                    if (e.Y < thumb.Y)
                        this.Value -= _viewSize;
                    else
                        this.Value += _viewSize;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                int trackH = this.Height;
                int thumbH = Math.Max(24, (int)((float)_viewSize / _maximum * trackH));
                int availableTrack = trackH - thumbH;
                if (availableTrack > 0)
                {
                    int deltaY = e.Y - dragStartMouseY;
                    int maxScroll = _maximum - _viewSize;
                    int deltaVal = (int)((float)deltaY / availableTrack * maxScroll);
                    this.Value = dragStartValue + deltaVal;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
            Point clientMouse = this.PointToClient(Cursor.Position);
            isHovered = this.ClientRectangle.Contains(clientMouse);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle thumb = GetThumbRectangle();
            if (thumb == Rectangle.Empty) return;

            Color thumbColor = isDragging
                ? Color.FromArgb(120, 120, 120)
                : (isHovered ? Color.FromArgb(96, 96, 96) : Color.FromArgb(64, 64, 64));

            using (GraphicsPath path = GetThumbPath(thumb, 3f))
            {
                using (SolidBrush b = new SolidBrush(thumbColor))
                {
                    g.FillPath(b, path);
                }
            }
        }

        private static GraphicsPath GetThumbPath(Rectangle rect, float radius)
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
    /// Native Win32 drag, resize, and modern DWM drop shadow helpers.
    /// </summary>
    public static class Win32Helper
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int HTCLIENT = 1;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        public const int CS_DROPSHADOW = 0x00020000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int WS_MAXIMIZEBOX = 0x00010000;

        public const int LVM_FIRST = 0x1000;
        public const int LVM_GETHEADER = LVM_FIRST + 31;
        public const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
        public const int LVM_GETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 55;
        public const int LVS_EX_DOUBLEBUFFER = 0x00010000;
        public const int WM_ERASEBKGND = 0x0014;

        // DWM Window Attributes
        public const int DWMWA_NCRENDERING_POLICY = 2;
        public const int DWMWA_ALLOW_NCPAINT = 4;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_BORDER_COLOR = 34;

        // DWM Values
        public const int DWMNCRP_ENABLED = 2;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;

        public static void DragWindow(IntPtr handle, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        /// <summary>
        /// Applies authentic hardware-accelerated Windows 11/10 DWM elevation drop shadow,
        /// rounded corners, and dark mode frame attributes to a borderless Form.
        /// </summary>
        public static void ApplyWindowShadow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                // Force DWM non-client rendering policy to enabled
                int ncrp = DWMNCRP_ENABLED;
                DwmSetWindowAttribute(hWnd, DWMWA_NCRENDERING_POLICY, ref ncrp, sizeof(int));

                // Allow non-client paint for shadow composition
                int allowNc = 1;
                DwmSetWindowAttribute(hWnd, DWMWA_ALLOW_NCPAINT, ref allowNc, sizeof(int));

                // Extend DWM frame into client area to activate the elevation drop shadow
                MARGINS margins = new MARGINS { leftWidth = 1, rightWidth = 1, topHeight = 1, bottomHeight = 1 };
                DwmExtendFrameIntoClientArea(hWnd, ref margins);

                // Enable Windows 11 rounded corners (DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND = 2)
                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                // Apply immersive dark mode frame attribute
                int darkMode = 1;
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref darkMode, sizeof(int));

                // Apply subtle Windows 11 border color (#3A3A3A -> 0x003A3A3A)
                int borderColor = 0x003A3A3A;
                DwmSetWindowAttribute(hWnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
            catch
            {
                // Graceful fallback on older OS versions or minimal environments
            }
        }
    }

    /// <summary>
    /// Global application message filter that enables fluid, native border and corner resizing
    /// on custom borderless WinForms windows (such as MainForm), even when covered by child panels.
    /// </summary>
    public class WindowResizeFilter : IMessageFilter
    {
        private readonly Form target;
        private readonly int borderMargin;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;

        public WindowResizeFilter(Form targetForm, int resizeBorder = 6)
        {
            this.target = targetForm;
            this.borderMargin = resizeBorder;
        }

        public int GetHitCode(Point screenPos)
        {
            if (target == null || target.IsDisposed || !target.Visible || target.WindowState == FormWindowState.Maximized)
                return 0;

            Point pt = target.PointToClient(screenPos);
            int b = borderMargin;

            // Check if cursor is near target form bounds
            if (pt.X < -b || pt.X > target.ClientSize.Width + b || pt.Y < -b || pt.Y > target.ClientSize.Height + b)
                return 0;

            // Never intercept caption buttons area (top-right controls: Min, Max, Close)
            if (pt.Y <= 32 && pt.X >= target.ClientSize.Width - 140)
                return 0;

            bool left = pt.X <= b;
            bool right = pt.X >= target.ClientSize.Width - b;
            bool top = pt.Y <= b;
            bool bottom = pt.Y >= target.ClientSize.Height - b;

            if (top && left) return Win32Helper.HTTOPLEFT;
            if (top && right) return Win32Helper.HTTOPRIGHT;
            if (bottom && left) return Win32Helper.HTBOTTOMLEFT;
            if (bottom && right) return Win32Helper.HTBOTTOMRIGHT;
            if (left) return Win32Helper.HTLEFT;
            if (right) return Win32Helper.HTRIGHT;
            if (top) return Win32Helper.HTTOP;
            if (bottom) return Win32Helper.HTBOTTOM;

            return 0;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (target == null || target.IsDisposed || !target.Visible || target.WindowState == FormWindowState.Maximized)
                return false;

            if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDOWN)
            {
                int hit = GetHitCode(Cursor.Position);
                if (hit != 0)
                {
                    if (m.Msg == WM_MOUSEMOVE)
                    {
                        if (hit == Win32Helper.HTLEFT || hit == Win32Helper.HTRIGHT)
                            Cursor.Current = Cursors.SizeWE;
                        else if (hit == Win32Helper.HTTOP || hit == Win32Helper.HTBOTTOM)
                            Cursor.Current = Cursors.SizeNS;
                        else if (hit == Win32Helper.HTTOPLEFT || hit == Win32Helper.HTBOTTOMRIGHT)
                            Cursor.Current = Cursors.SizeNWSE;
                        else if (hit == Win32Helper.HTTOPRIGHT || hit == Win32Helper.HTBOTTOMLEFT)
                            Cursor.Current = Cursors.SizeNESW;
                    }
                    else if (m.Msg == WM_LBUTTONDOWN)
                    {
                        Win32Helper.ReleaseCapture();
                        Win32Helper.SendMessage(target.Handle, Win32Helper.WM_NCLBUTTONDOWN, hit, 0);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Subclasses the native SysHeader32 window of a ListView to ensure that the unpopulated
    /// header region to the right of columns is rendered with the dark Fluent theme rather than
    /// the default Win32 white / light gray COLOR_BTNFACE background.
    /// </summary>
    public class HeaderNativeWindow : NativeWindow
    {
        private readonly ListView _owner;
        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public HeaderNativeWindow(ListView owner, IntPtr handle)
        {
            _owner = owner;
            this.AssignHandle(handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                if (m.WParam != IntPtr.Zero)
                {
                    using (Graphics g = Graphics.FromHdc(m.WParam))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(35, 35, 35)))
                    {
                        RECT rc;
                        GetClientRect(this.Handle, out rc);
                        g.FillRectangle(b, 0, 0, rc.Right - rc.Left, rc.Bottom - rc.Top);
                    }
                }
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                if (_owner != null && _owner.Columns.Count > 0)
                {
                    int totalColWidth = 0;
                    foreach (ColumnHeader ch in _owner.Columns) totalColWidth += ch.Width;

                    RECT rc;
                    GetClientRect(this.Handle, out rc);
                    int width = rc.Right - rc.Left;
                    int height = rc.Bottom - rc.Top;

                    if (totalColWidth < width)
                    {
                        using (Graphics g = Graphics.FromHwnd(this.Handle))
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(35, 35, 35)))
                        using (Pen p = new Pen(WinColors.Border, 1f))
                        {
                            Rectangle extraRect = new Rectangle(totalColWidth, 0, width - totalColWidth, height);
                            g.FillRectangle(b, extraRect);
                            g.DrawLine(p, totalColWidth, height - 1, width, height - 1);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// High-performance, flicker-free owner-drawn ListView implementing Windows 11 Fluent dark theme.
    /// Eliminates all white background erase flashing during mouse hover via double buffering,
    /// Win32 LVS_EX_DOUBLEBUFFER extended styling, WM_ERASEBKGND background suppression,
    /// and provides dedicated row hover tracking.
    /// </summary>
    public class FluentListView : ListView
    {
        private int _hoveredIndex = -1;
        private HeaderNativeWindow _headerSubclass;

        public int HoveredIndex
        {
            get { return _hoveredIndex; }
            set
            {
                if (_hoveredIndex != value)
                {
                    int old = _hoveredIndex;
                    _hoveredIndex = value;
                    if (old >= 0 && old < this.Items.Count)
                        this.RedrawItems(old, old, false);
                    if (_hoveredIndex >= 0 && _hoveredIndex < this.Items.Count)
                        this.RedrawItems(_hoveredIndex, _hoveredIndex, false);
                }
            }
        }

        public FluentListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }

        public void EnableDoubleBuffer()
        {
            if (this.IsHandleCreated)
            {
                int styles = Win32Helper.SendMessage(this.Handle, Win32Helper.LVM_GETEXTENDEDLISTVIEWSTYLE, 0, 0);
                styles |= Win32Helper.LVS_EX_DOUBLEBUFFER;
                Win32Helper.SendMessage(this.Handle, Win32Helper.LVM_SETEXTENDEDLISTVIEWSTYLE, 0, styles);
            }
        }

        public void ResetHover()
        {
            _hoveredIndex = -1;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnableDoubleBuffer();
            AttachHeaderSubclass();
        }

        public void AttachHeaderSubclass()
        {
            if (this.IsHandleCreated && _headerSubclass == null)
            {
                IntPtr hHeader = (IntPtr)Win32Helper.SendMessage(this.Handle, Win32Helper.LVM_GETHEADER, 0, 0);
                if (hHeader != IntPtr.Zero)
                {
                    _headerSubclass = new HeaderNativeWindow(this, hHeader);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ListViewHitTestInfo hit = this.HitTest(e.Location);
            int newHover = (hit.Item != null) ? hit.Item.Index : -1;
            this.HoveredIndex = newHover;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.HoveredIndex = -1;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32Helper.WM_ERASEBKGND)
            {
                if (m.WParam != IntPtr.Zero)
                {
                    using (Graphics g = Graphics.FromHdc(m.WParam))
                    using (SolidBrush b = new SolidBrush(this.BackColor))
                    {
                        g.FillRectangle(b, this.ClientRectangle);
                    }
                }
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_headerSubclass != null)
            {
                _headerSubclass.ReleaseHandle();
                _headerSubclass = null;
            }
            base.OnHandleDestroyed(e);
        }
    }

    /// <summary>
    /// Modern Windows 11 Fluent dark dropdown combo box.
    /// Features custom dark background, smooth hover, 1px subtle border, accent focus outline,
    /// a crisp chevron icon, and a dark themed popup menu.
    /// </summary>
    public class ModernComboBox : Control
    {
        public class ItemCollection : System.Collections.IList
        {
            private readonly ModernComboBox _owner;
            private readonly List<object> _list = new List<object>();

            public ItemCollection(ModernComboBox owner)
            {
                _owner = owner;
            }

            public int Count { get { return _list.Count; } }
            public object this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = value; _owner.Invalidate(); }
            }

            public int Add(object item)
            {
                _list.Add(item);
                _owner.Invalidate();
                return _list.Count - 1;
            }

            public void AddRange(object[] items)
            {
                if (items == null) return;
                _list.AddRange(items);
                _owner.Invalidate();
            }

            public void AddRange(string[] items)
            {
                if (items == null) return;
                foreach (var it in items) _list.Add(it);
                _owner.Invalidate();
            }

            public void Clear()
            {
                _list.Clear();
                _owner.SelectedIndex = -1;
                _owner.Invalidate();
            }

            public bool Contains(object item) { return _list.Contains(item); }
            public int IndexOf(object item) { return _list.IndexOf(item); }
            public void Insert(int index, object item) { _list.Insert(index, item); _owner.Invalidate(); }
            public void Remove(object item) { _list.Remove(item); _owner.Invalidate(); }
            public void RemoveAt(int index) { _list.RemoveAt(index); _owner.Invalidate(); }

            public System.Collections.IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
            public void CopyTo(Array array, int index) { ((System.Collections.ICollection)_list).CopyTo(array, index); }
            public bool IsFixedSize { get { return false; } }
            public bool IsReadOnly { get { return false; } }
            public bool IsSynchronized { get { return false; } }
            public object SyncRoot { get { return this; } }
        }

        private readonly ItemCollection _items;
        private int _selectedIndex = -1;
        private bool _isHovered = false;
        private bool _isOpen = false;
        private ContextMenuStrip _dropdownMenu;

        public event EventHandler SelectedIndexChanged;

        public ItemCollection Items { get { return _items; } }

        public ComboBoxStyle DropDownStyle { get; set; }
        public FlatStyle FlatStyle { get; set; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int newIndex = value;
                if (newIndex < -1 || newIndex >= _items.Count) newIndex = -1;
                if (_selectedIndex != newIndex)
                {
                    _selectedIndex = newIndex;
                    Invalidate();
                    if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }

        public object SelectedItem
        {
            get { return (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null; }
            set
            {
                int idx = _items.IndexOf(value);
                SelectedIndex = idx;
            }
        }

        public new string Text
        {
            get { return SelectedItem != null ? SelectedItem.ToString() : string.Empty; }
            set
            {
                int idx = _items.IndexOf(value);
                if (idx >= 0) SelectedIndex = idx;
            }
        }

        public ModernComboBox()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor,
                true
            );

            this.DropDownStyle = ComboBoxStyle.DropDownList;
            this.FlatStyle = FlatStyle.Flat;
            _items = new ItemCollection(this);
            this.Size = new Size(180, 28);
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.ForeColor = WinColors.TextSecondary;
            this.BackColor = Color.FromArgb(46, 46, 46);
            this.DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
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

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!this.Enabled) return;
            ToggleDropDown();
        }

        private void ToggleDropDown()
        {
            if (_isOpen)
            {
                if (_dropdownMenu != null) _dropdownMenu.Close();
                return;
            }

            if (_items.Count == 0) return;

            _dropdownMenu = new ContextMenuStrip();
            _dropdownMenu.Renderer = new FluentDropDownRenderer();
            _dropdownMenu.BackColor = Color.FromArgb(36, 36, 36);
            _dropdownMenu.ShowImageMargin = false;
            _dropdownMenu.ShowCheckMargin = false;
            _dropdownMenu.Font = this.Font;
            _dropdownMenu.AutoSize = false;
            _dropdownMenu.Width = Math.Max(this.Width, 180);
            _dropdownMenu.Height = (_items.Count * 30) + 8;
            _dropdownMenu.Padding = new Padding(2, 4, 2, 4);

            for (int i = 0; i < _items.Count; i++)
            {
                int index = i;
                ToolStripMenuItem item = new ToolStripMenuItem(_items[i] != null ? _items[i].ToString() : string.Empty);
                item.AutoSize = false;
                item.Width = _dropdownMenu.Width - 4;
                item.Height = 28;
                item.TextAlign = ContentAlignment.MiddleLeft;
                item.Padding = new Padding(12, 0, 8, 0);
                bool isSel = (index == _selectedIndex);
                item.ForeColor = isSel ? WinColors.Accent : WinColors.TextWhite;
                item.Font = isSel ? new Font(this.Font, FontStyle.Bold) : this.Font;
                item.Click += delegate
                {
                    this.SelectedIndex = index;
                };
                _dropdownMenu.Items.Add(item);
            }

            _isOpen = true;
            Invalidate();

            _dropdownMenu.Closed += delegate
            {
                _isOpen = false;
                Invalidate();
            };

            _dropdownMenu.Show(this, new Point(0, this.Height + 2));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!this.Enabled) return base.ProcessCmdKey(ref msg, keyData);

            if (keyData == Keys.Down || keyData == (Keys.Alt | Keys.Down))
            {
                if (!_isOpen) ToggleDropDown();
                else if (SelectedIndex < _items.Count - 1) SelectedIndex++;
                return true;
            }
            if (keyData == Keys.Up)
            {
                if (SelectedIndex > 0) SelectedIndex--;
                return true;
            }
            if (keyData == Keys.Enter || keyData == Keys.Space)
            {
                ToggleDropDown();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!this.Enabled || _items.Count == 0) return;
            if (e.Delta < 0 && SelectedIndex < _items.Count - 1)
            {
                SelectedIndex++;
            }
            else if (e.Delta > 0 && SelectedIndex > 0)
            {
                SelectedIndex--;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Default;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parentBg = this.Parent != null ? this.Parent.BackColor : WinColors.Card;
            g.Clear(parentBg);

            RectangleF rect = new RectangleF(0.5f, 0.5f, this.Width - 1f, this.Height - 1f);

            Color currentBg = !this.Enabled 
                ? Color.FromArgb(34, 34, 34) 
                : (_isOpen || _isHovered ? Color.FromArgb(56, 56, 56) : Color.FromArgb(46, 46, 46));

            Color currentBorder = !this.Enabled 
                ? Color.FromArgb(48, 48, 48) 
                : (_isOpen || this.Focused ? WinColors.Accent : (_isHovered ? Color.FromArgb(80, 80, 80) : WinColors.BorderLight));

            using (GraphicsPath path = GetRoundedRectanglePath(rect, 4.0f))
            {
                using (SolidBrush b = new SolidBrush(currentBg))
                {
                    g.FillPath(b, path);
                }

                using (Pen p = new Pen(currentBorder, (_isOpen || this.Focused) ? 1.5f : 1f))
                {
                    p.Alignment = PenAlignment.Inset;
                    g.DrawPath(p, path);
                }
            }

            // Draw text
            string textToDraw = (_selectedIndex >= 0 && _selectedIndex < _items.Count && _items[_selectedIndex] != null) 
                ? _items[_selectedIndex].ToString() 
                : string.Empty;

            int chevronWidth = 24;
            Rectangle textRect = new Rectangle(10, 0, this.Width - chevronWidth - 8, this.Height);
            Color textClr = !this.Enabled ? WinColors.TextSubtle : (this.Focused || _isHovered || _isOpen ? WinColors.TextWhite : this.ForeColor);

            TextRenderer.DrawText(
                g,
                textToDraw,
                this.Font,
                textRect,
                textClr,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
            );

            // Draw Chevron Down Icon
            int chX = this.Width - chevronWidth;
            int chY = (this.Height - 12) / 2;
            Bitmap chBmp = IconResources.GetIcon("chevron_down");
            if (chBmp != null)
            {
                Color chColor = !this.Enabled ? WinColors.TextSubtle : (_isOpen ? WinColors.Accent : (_isHovered ? WinColors.TextWhite : WinColors.TextMuted));
                IconHelper.DrawTintedIcon(g, chBmp, new Rectangle(chX + 2, chY, 12, 12), chColor);
            }
            else
            {
                using (Pen chPen = new Pen(_isOpen ? WinColors.Accent : WinColors.TextMuted, 1.5f))
                {
                    chPen.StartCap = LineCap.Round;
                    chPen.EndCap = LineCap.Round;
                    int cx = chX + 8;
                    int cy = this.Height / 2 - 1;
                    g.DrawLine(chPen, cx - 4, cy, cx, cy + 4);
                    g.DrawLine(chPen, cx, cy + 4, cx + 4, cy);
                }
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(RectangleF rect, float radius)
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

    internal class FluentDropDownRenderer : ToolStripProfessionalRenderer
    {
        public FluentDropDownRenderer() : base(new FluentColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);

            if (e.Item.Selected)
            {
                using (GraphicsPath path = GetRoundedRect(rect, 4f))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(58, 58, 58)))
                {
                    g.FillPath(b, path);
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(36, 36, 36)))
                {
                    g.FillRectangle(b, e.Item.ContentRectangle);
                }
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(WinColors.Border, 1f))
            {
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, float radius)
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

    internal class FluentColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(36, 36, 36); } }
        public override Color MenuBorder { get { return WinColors.Border; } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(58, 58, 58); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(36, 36, 36); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(36, 36, 36); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(36, 36, 36); } }
    }
}
