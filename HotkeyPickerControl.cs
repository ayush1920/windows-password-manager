using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Microsoft PowerToys-style interactive Hotkey Picker control.
    /// Allows the user to click and directly press their desired keyboard shortcut.
    /// Captures multiple modifiers (Win, Ctrl, Alt, Shift) and primary keys,
    /// performs live Win32 API availability validation, and displays rich visual badge pills.
    /// </summary>
    public class HotkeyPickerControl : Control
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private int selectedModifiers;
        private Keys selectedKey;
        private bool isListening = false;
        private string warningMessage = null;

        public event EventHandler HotkeyChanged;

        public int SelectedModifiers
        {
            get { return selectedModifiers; }
            set
            {
                selectedModifiers = value;
                ValidateCurrentHotkey();
                Invalidate();
            }
        }

        public Keys SelectedKey
        {
            get { return selectedKey; }
            set
            {
                selectedKey = value;
                ValidateCurrentHotkey();
                Invalidate();
            }
        }

        public string WarningMessage
        {
            get { return warningMessage; }
        }

        public bool HasConflict
        {
            get { return !string.IsNullOrEmpty(warningMessage); }
        }

        public HotkeyPickerControl()
        {
            this.Size = new Size(340, 42);
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

            // Default: Ctrl + Alt + K
            this.selectedModifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT;
            this.selectedKey = Keys.K;
        }

        public void SetHotkey(int modifiers, Keys key)
        {
            this.selectedModifiers = modifiers;
            this.selectedKey = key;
            ValidateCurrentHotkey();
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            this.Focus();
            isListening = true;
            Invalidate();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            isListening = true;
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            isListening = false;
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            // Intercept all keys including arrows, tab, and function keys when recording
            return isListening || base.IsInputKey(keyData);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (isListening)
            {
                e.IsInputKey = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!isListening)
            {
                base.OnKeyDown(e);
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;

            // Detect Win Key state
            bool winPressed = ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0) || ((GetAsyncKeyState(VK_RWIN) & 0x8000) != 0);

            int mods = 0;
            if (winPressed) mods |= GlobalHotkeyManager.MOD_WIN;
            if (e.Control) mods |= GlobalHotkeyManager.MOD_CONTROL;
            if (e.Alt) mods |= GlobalHotkeyManager.MOD_ALT;
            if (e.Shift) mods |= GlobalHotkeyManager.MOD_SHIFT;

            Keys pureKey = e.KeyCode;

            // Check if user pressed Escape to cancel
            if (pureKey == Keys.Escape && mods == 0)
            {
                isListening = false;
                Invalidate();
                return;
            }

            // Ignore lone modifier presses until a primary action key is struck
            bool isModifierOnly = (pureKey == Keys.ControlKey || pureKey == Keys.LControlKey || pureKey == Keys.RControlKey ||
                                   pureKey == Keys.Menu || pureKey == Keys.LMenu || pureKey == Keys.RMenu ||
                                   pureKey == Keys.ShiftKey || pureKey == Keys.LShiftKey || pureKey == Keys.RShiftKey ||
                                   pureKey == Keys.LWin || pureKey == Keys.RWin);

            if (!isModifierOnly)
            {
                // Ensure at least one modifier key is included for a valid global hotkey
                if (mods == 0)
                {
                    mods = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT; // Default fallback modifier
                }

                this.selectedModifiers = mods;
                this.selectedKey = pureKey;
                this.isListening = false;

                ValidateCurrentHotkey();

                if (HotkeyChanged != null)
                {
                    HotkeyChanged(this, EventArgs.Empty);
                }

                Invalidate();
            }
            else
            {
                // Update live feedback of pressed modifiers while waiting for main key
                this.selectedModifiers = mods;
                Invalidate();
            }
        }

        public void ValidateCurrentHotkey()
        {
            if (this.IsHandleCreated && selectedKey != Keys.None)
            {
                string warn;
                bool ok = GlobalHotkeyManager.TestHotkeyAvailability(this.Handle, selectedModifiers, selectedKey, out warn);
                this.warningMessage = ok ? null : warn;
            }
            else
            {
                this.warningMessage = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // Background
            Color bg = isListening ? Color.FromArgb(28, 33, 46) : Color.FromArgb(20, 24, 33);
            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillPath(b, GetRoundedRectangle(rect, 4f));
            }

            // Border (Glows indigo when listening)
            Color borderColor = isListening 
                ? Color.FromArgb(99, 102, 241) 
                : (!string.IsNullOrEmpty(warningMessage) ? Color.FromArgb(239, 68, 68) : Color.FromArgb(37, 44, 65));

            using (Pen p = new Pen(borderColor, isListening ? 1.8f : 1f))
            {
                g.DrawPath(p, GetRoundedRectangle(rect, 4f));
            }

            if (isListening && selectedKey == Keys.None)
            {
                // Prompt text
                string prompt = "Press desired shortcut keys (e.g. Win + Alt + K)...";
                using (Font f = new Font("Segoe UI", 9f, FontStyle.Italic))
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(129, 140, 248)))
                {
                    g.DrawString(prompt, f, tb, 14, (this.Height - 16) / 2);
                }
            }
            else
            {
                // Draw PowerToys-style Badge Pills
                int x = 12;
                int pillY = (this.Height - 24) / 2;

                if ((selectedModifiers & GlobalHotkeyManager.MOD_WIN) != 0)
                {
                    x = DrawPill(g, "Win", x, pillY) + 6;
                    x = DrawPlus(g, x, pillY) + 6;
                }
                if ((selectedModifiers & GlobalHotkeyManager.MOD_CONTROL) != 0)
                {
                    x = DrawPill(g, "Ctrl", x, pillY) + 6;
                    x = DrawPlus(g, x, pillY) + 6;
                }
                if ((selectedModifiers & GlobalHotkeyManager.MOD_ALT) != 0)
                {
                    x = DrawPill(g, "Alt", x, pillY) + 6;
                    x = DrawPlus(g, x, pillY) + 6;
                }
                if ((selectedModifiers & GlobalHotkeyManager.MOD_SHIFT) != 0)
                {
                    x = DrawPill(g, "Shift", x, pillY) + 6;
                    x = DrawPlus(g, x, pillY) + 6;
                }

                if (selectedKey != Keys.None)
                {
                    DrawPill(g, GlobalHotkeyManager.FormatKeyName(selectedKey), x, pillY, true);
                }
                else if (isListening)
                {
                    using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Italic))
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(156, 163, 175)))
                    {
                        g.DrawString("+ [Press Key]", f, tb, x, pillY + 3);
                    }
                }
            }

            // Click hint on the right
            string hint = isListening ? "Listening..." : "Click to rebind";
            using (Font f = new Font("Segoe UI", 8f))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(107, 114, 128)))
            {
                SizeF size = g.MeasureString(hint, f);
                g.DrawString(hint, f, tb, this.Width - size.Width - 12, (this.Height - size.Height) / 2);
            }
        }

        private int DrawPill(Graphics g, string text, int x, int y, bool isPrimaryKey = false)
        {
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(text, f);
                int pillWidth = (int)size.Width + 14;
                int pillHeight = 24;

                Rectangle pillRect = new Rectangle(x, y, pillWidth, pillHeight);
                Color pillBg = isPrimaryKey ? Color.FromArgb(49, 46, 129) : Color.FromArgb(31, 35, 48);
                Color pillBorder = isPrimaryKey ? Color.FromArgb(99, 102, 241) : Color.FromArgb(55, 65, 81);
                Color textColor = isPrimaryKey ? Color.FromArgb(224, 231, 255) : Color.FromArgb(243, 244, 246);

                using (SolidBrush b = new SolidBrush(pillBg))
                using (Pen p = new Pen(pillBorder, 1f))
                {
                    GraphicsPath path = GetRoundedRectangle(pillRect, 3.5f);
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }

                using (SolidBrush tb = new SolidBrush(textColor))
                {
                    g.DrawString(text, f, tb, x + 7, y + 3);
                }

                return x + pillWidth;
            }
        }

        private int DrawPlus(Graphics g, int x, int y)
        {
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(156, 163, 175)))
            {
                g.DrawString("+", f, tb, x, y + 3);
                return x + 10;
            }
        }

        private static GraphicsPath GetRoundedRectangle(Rectangle rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
