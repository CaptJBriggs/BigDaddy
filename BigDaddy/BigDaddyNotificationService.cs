using System;
using System.Drawing;
using System.Windows.Forms;

namespace BigDaddy
{
    // Uses Windows' built-in notification area to announce the start of enabled breaks.
    internal class BigDaddyNotificationService : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripMenuItem statusMenuItem;

        public event EventHandler? ShowRequested;
        public event EventHandler? ExitRequested;

        public BigDaddyNotificationService()
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            statusMenuItem = new ToolStripMenuItem("Big Daddy is starting...")
            {
                Enabled = false
            };

            ToolStripMenuItem showMenuItem = new ToolStripMenuItem("Show Big Daddy");
            showMenuItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit Big Daddy");
            exitMenuItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

            trayMenu.Items.Add(statusMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(exitMenuItem);

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "Big Daddy",
                ContextMenuStrip = trayMenu
            };

            notifyIcon.MouseClick += NotifyIcon_MouseClick;
            notifyIcon.Visible = true;
        }

        // Lets the left mouse button open the settings window while the right button keeps
        // Windows' normal context-menu behaviour.
        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // Keeps the native tray tooltip and menu status in sync with the scheduler.
        public void UpdateStatus(string status, TimeSpan remainingTime)
        {
            string displayStatus = $"{status} - {remainingTime:hh\\:mm\\:ss}";
            statusMenuItem.Text = displayStatus;
            notifyIcon.Text = displayStatus.Length <= 63
                ? displayStatus
                : displayStatus[..63];
        }

        // Displays a Windows notification without interrupting the countdown or opening a custom form.
        public void ShowBreakStartedNotification(string breakName)
        {
            notifyIcon.ShowBalloonTip(
                5000,
                "Big Daddy",
                $"{breakName} has started.",
                ToolTipIcon.Info);
        }

        // Removes the tray icon when the application closes.
        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.MouseClick -= NotifyIcon_MouseClick;
            notifyIcon.Dispose();
        }
    }
}
