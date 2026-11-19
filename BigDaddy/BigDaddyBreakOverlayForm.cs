using System.Drawing;
using System.Windows.Forms;

namespace BigDaddy
{
    // Covers the main screen while a scheduled break is active.
    internal class BigDaddyBreakOverlayForm : Form
    {
        private readonly Label messageLabel = new Label();
        private readonly Label countdownLabel = new Label();
        private readonly Button unlockKeyboardButton = new Button();
        private readonly Button shutdownButton = new Button();
        private readonly Button signOutButton = new Button();
        private readonly Button switchUserButton = new Button();
        private readonly BigDaddyKeyboardBlocker keyboardBlocker = new BigDaddyKeyboardBlocker();
        private bool keyboardMustBeBlocked;

        public event EventHandler? KeyboardUnlockRequested;
        public event EventHandler? ShutdownRequested;
        public event EventHandler? SignOutRequested;
        public event EventHandler? SwitchUserRequested;

        public BigDaddyBreakOverlayForm()
        {
            BackColor = Color.FromArgb(24, 24, 24);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            WindowState = FormWindowState.Maximized;

            messageLabel.Dock = DockStyle.Fill;
            messageLabel.ForeColor = Color.White;
            messageLabel.Font = new Font("Segoe UI", 28F, FontStyle.Regular, GraphicsUnit.Point);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(messageLabel);

            countdownLabel.AutoSize = false;
            countdownLabel.Font = new Font("Segoe UI", 36F, FontStyle.Bold, GraphicsUnit.Point);
            countdownLabel.ForeColor = Color.White;
            countdownLabel.Size = new Size(360, 60);
            countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
            countdownLabel.Text = "00:00:00";
            Controls.Add(countdownLabel);

            unlockKeyboardButton.Anchor = AnchorStyles.Bottom;
            unlockKeyboardButton.Location = new Point(0, 0);
            unlockKeyboardButton.Size = new Size(200, 32);
            unlockKeyboardButton.Text = "Emergency Keyboard Access";
            unlockKeyboardButton.Click += UnlockKeyboardButton_Click;
            Controls.Add(unlockKeyboardButton);

            shutdownButton.Size = new Size(120, 32);
            shutdownButton.Text = "Shut Down...";
            shutdownButton.Click += ShutdownButton_Click;
            Controls.Add(shutdownButton);

            signOutButton.Size = new Size(100, 32);
            signOutButton.Text = "Sign Out...";
            signOutButton.Click += SignOutButton_Click;
            Controls.Add(signOutButton);

            switchUserButton.Size = new Size(110, 32);
            switchUserButton.Text = "Switch User...";
            switchUserButton.Click += SwitchUserButton_Click;
            Controls.Add(switchUserButton);

            FormClosing += BigDaddyBreakOverlayForm_FormClosing;
            FormClosed += BigDaddyBreakOverlayForm_FormClosed;
            Resize += BigDaddyBreakOverlayForm_Resize;
        }

        // Shows the active break name in the full-screen overlay.
        public void ShowBreak(
            IWin32Window owner,
            string breakName,
            string customMessage,
            bool requireKeyboardBlock,
            TimeSpan remainingTime)
        {
            string message = string.IsNullOrWhiteSpace(customMessage)
                ? "Please take a moment away from the screen."
                : customMessage;

            messageLabel.Text = $"{breakName}\n\n{message}";
            UpdateCountdown(remainingTime);
            keyboardMustBeBlocked = requireKeyboardBlock;
            unlockKeyboardButton.Visible = requireKeyboardBlock;
            shutdownButton.Visible = true;
            PositionActionButtons();

            if (!Visible)
            {
                Show(owner);
            }

            BringToFront();
            PositionActionButtons();
            messageLabel.SendToBack();
            countdownLabel.BringToFront();
            shutdownButton.BringToFront();
            signOutButton.BringToFront();
            switchUserButton.BringToFront();
            unlockKeyboardButton.BringToFront();
            UpdateKeyboardBlock();
        }

        // Removes the overlay as soon as the next play interval begins.
        public void HideBreak()
        {
            keyboardMustBeBlocked = false;
            keyboardBlocker.Stop();
            Hide();
        }

        // Matches the full-screen break surface to the selected application appearance.
        public void ApplyTheme(bool useDarkTheme)
        {
            BackColor = useDarkTheme ? Color.FromArgb(24, 28, 33) : Color.FromArgb(242, 245, 248);
            Color textColor = useDarkTheme ? Color.White : Color.FromArgb(30, 35, 42);
            Color buttonColor = useDarkTheme ? Color.FromArgb(36, 69, 76) : Color.FromArgb(222, 237, 238);

            messageLabel.ForeColor = textColor;
            countdownLabel.ForeColor = textColor;

            foreach (Button button in new[] { unlockKeyboardButton, shutdownButton, signOutButton, switchUserButton })
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = buttonColor;
                button.ForeColor = textColor;
            }
        }

        // Displays the scheduler's live remaining time without taking ownership of timing.
        public void UpdateCountdown(TimeSpan remainingTime)
        {
            countdownLabel.Text = remainingTime.ToString(@"hh\:mm\:ss");
            PositionCountdown();
        }

        // Lets a verified emergency PIN restore keyboard access without dismissing the overlay.
        public void UnlockKeyboard()
        {
            keyboardMustBeBlocked = false;
            keyboardBlocker.Stop();
            unlockKeyboardButton.Visible = false;
            PositionActionButtons();
        }

        private void UnlockKeyboardButton_Click(object? sender, EventArgs e)
        {
            KeyboardUnlockRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ShutdownButton_Click(object? sender, EventArgs e)
        {
            ShutdownRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SignOutButton_Click(object? sender, EventArgs e)
        {
            SignOutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SwitchUserButton_Click(object? sender, EventArgs e)
        {
            SwitchUserRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateKeyboardBlock()
        {
            if (keyboardMustBeBlocked)
            {
                keyboardBlocker.Start();
            }
            else
            {
                keyboardBlocker.Stop();
            }
        }

        // Keeps the overlay in place while the scheduler has an active break.
        private void BigDaddyBreakOverlayForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }

        private void BigDaddyBreakOverlayForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            keyboardBlocker.Dispose();
        }

        private void BigDaddyBreakOverlayForm_Resize(object? sender, EventArgs e)
        {
            PositionActionButtons();
            PositionCountdown();
        }

        private void PositionActionButtons()
        {
            int totalWidth = shutdownButton.Width + signOutButton.Width + switchUserButton.Width + 20 +
                (unlockKeyboardButton.Visible ? unlockKeyboardButton.Width + 10 : 0);
            int left = (ClientSize.Width - totalWidth) / 2;

            shutdownButton.Location = new Point(left, ClientSize.Height - 70);

            signOutButton.Location = new Point(left + shutdownButton.Width + 10, ClientSize.Height - 70);

            switchUserButton.Location = new Point(
                signOutButton.Right + 10,
                ClientSize.Height - 70);

            if (unlockKeyboardButton.Visible)
            {
                unlockKeyboardButton.Location = new Point(
                    switchUserButton.Right + 10,
                    ClientSize.Height - 70);
            }
        }

        private void PositionCountdown()
        {
            countdownLabel.Location = new Point(
                (ClientSize.Width - countdownLabel.Width) / 2,
                Math.Max(20, ClientSize.Height - 155));
        }
    }
}
