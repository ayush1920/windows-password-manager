using System;
using System.Drawing;
using System.Windows.Forms;

namespace PasswordGui
{
    /// <summary>
    /// Windows 11 Fluent Modal Dialog prompting the user to either Create a New Vault
    /// or Open an Existing Vault when attempting to add credentials without a loaded database.
    /// </summary>
    public class VaultActionDialog : Form
    {
        public VaultActionDialog()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(490, 208);
            this.BackColor = WinColors.Window;
            this.ForeColor = WinColors.TextWhite;
            this.Font = new Font("Segoe UI", 9f);
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            // 1px Border
            this.Paint += delegate (object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(WinColors.Border, 1f))
                {
                    pe.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // Custom Title Bar (Height 36)
            Panel pnlTitle = new Panel();
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Height = 36;
            pnlTitle.BackColor = WinColors.Window;
            pnlTitle.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);

            Panel pnlIcon = new Panel();
            pnlIcon.Location = new Point(14, 10);
            pnlIcon.Size = new Size(16, 16);
            pnlIcon.BackColor = Color.Transparent;
            pnlIcon.Paint += delegate (object s, PaintEventArgs pe)
            {
                Bitmap bmp = IconResources.GetIcon("database");
                if (bmp != null) IconHelper.DrawTintedIcon(pe.Graphics, bmp, new Rectangle(0, 0, 16, 16), WinColors.Accent);
            };
            pnlTitle.Controls.Add(pnlIcon);

            Label lblTitle = new Label();
            lblTitle.Text = "KeyCraft — No Vault Loaded";
            lblTitle.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblTitle.ForeColor = WinColors.TextMuted;
            lblTitle.Location = new Point(38, 9);
            lblTitle.AutoSize = true;
            lblTitle.MouseDown += (s, e) => Win32Helper.DragWindow(this.Handle, e);
            pnlTitle.Controls.Add(lblTitle);

            TitleBarButton btnClose = new TitleBarButton(TitleButtonType.Close);
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 42;
            btnClose.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlTitle.Controls.Add(btnClose);

            // Body Panel
            Panel pnlBody = new Panel();
            pnlBody.Dock = DockStyle.Fill;
            pnlBody.BackColor = WinColors.Window;
            pnlBody.Padding = new Padding(24, 12, 24, 16);

            Label lblHeader = new Label();
            lblHeader.Text = "No Vault File Loaded";
            lblHeader.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblHeader.ForeColor = WinColors.TextWhite;
            lblHeader.Location = new Point(24, 14);
            lblHeader.AutoSize = true;
            pnlBody.Controls.Add(lblHeader);

            Label lblSub = new Label();
            lblSub.Text = "To add and store credentials, please create a new encrypted vault or open an existing file.";
            lblSub.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblSub.ForeColor = WinColors.TextSecondary;
            lblSub.Location = new Point(24, 44);
            lblSub.MaximumSize = new Size(440, 0);
            lblSub.AutoSize = true;
            pnlBody.Controls.Add(lblSub);

            // Action Buttons
            Panel pnlActions = new Panel();
            pnlActions.Location = new Point(24, 104);
            pnlActions.Size = new Size(440, 42);
            pnlActions.BackColor = Color.Transparent;

            ModernButton btnCreate = new ModernButton();
            btnCreate.Text = "Create New Vault";
            btnCreate.IconName = "plus";
            btnCreate.IconSize = 12;
            btnCreate.NormalBg = WinColors.Accent;
            btnCreate.HoverBg = WinColors.AccentHover;
            btnCreate.PressedBg = WinColors.AccentPressed;
            btnCreate.BorderColor = WinColors.Accent;
            btnCreate.NormalFg = Color.White;
            btnCreate.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnCreate.Size = new Size(160, 34);
            btnCreate.Location = new Point(0, 4);
            btnCreate.Click += delegate
            {
                this.DialogResult = DialogResult.Yes;
                this.Close();
            };
            pnlActions.Controls.Add(btnCreate);

            ModernButton btnOpen = new ModernButton();
            btnOpen.Text = "Open Vault";
            btnOpen.IconName = "folder";
            btnOpen.IconSize = 12;
            btnOpen.Size = new Size(120, 34);
            btnOpen.Location = new Point(172, 4);
            btnOpen.Click += delegate
            {
                this.DialogResult = DialogResult.No;
                this.Close();
            };
            pnlActions.Controls.Add(btnOpen);

            ModernButton btnCancel = new ModernButton();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(90, 34);
            btnCancel.Location = new Point(304, 4);
            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            pnlActions.Controls.Add(btnCancel);

            pnlBody.Controls.Add(pnlActions);

            // Natural WinForms docking order: Add Fill first, then Top so Top docks first and Fill gets remaining space without overlap
            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlTitle);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= Win32Helper.CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win32Helper.ApplyWindowShadow(this.Handle);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
