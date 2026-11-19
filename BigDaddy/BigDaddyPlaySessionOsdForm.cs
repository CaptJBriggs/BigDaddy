using System.Drawing;
using System.Windows.Forms;

namespace BigDaddy
{
    // A non-activating, compact display for the current Playing countdown.
    internal sealed class BigDaddyPlaySessionOsdForm : Form
    {
        private readonly Label timeLabel = new Label();
        private bool alternateWarningColour;
        private Color normalTextColor = Color.White;

        public BigDaddyPlaySessionOsdForm()
        {
            BackColor = Color.FromArgb(28, 28, 28);
            ClientSize = new Size(190, 68);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            timeLabel.Dock = DockStyle.Fill;
            timeLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            timeLabel.ForeColor = Color.White;
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(timeLabel);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WsExNoActivate = 0x08000000;
                const int WsExToolWindow = 0x00000080;
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExNoActivate | WsExToolWindow;
                return parameters;
            }
        }

        public void ShowPlayTime(
            TimeSpan remainingTime,
            bool warning,
            int currentPlaySession,
            int sessionsPerLongBreak,
            int completedLongBreaksToday)
        {
            timeLabel.Text = $"{remainingTime:hh\\:mm\\:ss}\n" +
                $"Play session {currentPlaySession} of {sessionsPerLongBreak}\n" +
                $"Long breaks today: {completedLongBreaksToday}";
            alternateWarningColour = warning && !alternateWarningColour;
            timeLabel.ForeColor = warning
                ? alternateWarningColour ? Color.Gold : Color.OrangeRed
                : normalTextColor;

            Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Location = new Point(workArea.Left + 8, workArea.Top + 8);

            if (!Visible)
            {
                Show();
            }
        }

        public void HideOsd()
        {
            alternateWarningColour = false;
            Hide();
        }

        // Matches the compact timer to the selected application appearance.
        public void ApplyTheme(bool useDarkTheme, bool useNeonTheme = false)
        {
            BackColor = useNeonTheme ? Color.FromArgb(14, 21, 27) :
                useDarkTheme ? Color.FromArgb(28, 31, 36) : Color.FromArgb(242, 245, 248);
            normalTextColor = useNeonTheme ? Color.FromArgb(211, 255, 248) :
                useDarkTheme ? Color.White : Color.FromArgb(30, 35, 42);
            timeLabel.ForeColor = normalTextColor;
        }
    }
}
