using Microsoft.Win32;

namespace BigDaddy
{
    // Owns the Windows Forms interface and passes validated input to the scheduler.
    public partial class BigDaddyForm : Form
    {
        // Settings object shared with the scheduler after user input is validated.
        private BigDaddySettings settings = new BigDaddySettings();
        // Handles saving and restoring the user's last valid settings.
        private BigDaddySettingsStore settingsStore = new BigDaddySettingsStore();
        // Saves the active countdown so a break can resume after shutdown or restart.
        private BigDaddySessionStateStore sessionStateStore = new BigDaddySessionStateStore();
        // Sends break-start messages through Windows' notification area.
        private BigDaddyNotificationService notificationService = new BigDaddyNotificationService();
        // Covers the screen while a scheduled break is active.
        private BigDaddyBreakOverlayForm breakOverlay = new BigDaddyBreakOverlayForm();
        // Shows an optional compact play-session countdown above other applications.
        private BigDaddyPlaySessionOsdForm playSessionOsd = new BigDaddyPlaySessionOsdForm();
        // Controls the play/short-break/long-break sequence.
        private BigDaddyScheduler scheduler;

        // Shows the current schedule phase beneath the countdown.
        private Label lblScheduleStatus = new Label();

        // Allows the user to set how long inactivity lasts before play pauses.
        private Label lblIdleThreshold = new Label();
        private BigDaddyTimeBox txtIdleThreshold = new BigDaddyTimeBox();

        // Allow either break type to be skipped without deleting its saved time values.
        private CheckBox chkShortBreaksEnabled = new CheckBox();
        private CheckBox chkLongBreaksEnabled = new CheckBox();
        private CheckBox chkShortBreakNotificationsEnabled = new CheckBox();
        private CheckBox chkLongBreakNotificationsEnabled = new CheckBox();
        private CheckBox chkAutomaticLongBreakShutdown = new CheckBox();
        private Button btnShortBreakMessage = new Button();
        private Button btnLongBreakMessage = new Button();
        private Button btnSetEmergencyPin = new Button();
        private Button btnClearEmergencyPin = new Button();
        private Button btnShutdown = new Button();
        private Button btnSignOut = new Button();
        private Button btnSwitchUser = new Button();
        private Button btnStart = new Button();
        private Button btnPause = new Button();
        private Button btnStop = new Button();
        private CheckBox chkPlaySessionOsdEnabled = new CheckBox();
        private CheckBox chkSendEscapeAtBreakStart = new CheckBox();
        private Label lblFlashWarningStart = new Label();
        private BigDaddyTimeBox txtFlashWarningStart = new BigDaddyTimeBox();
        private Label lblAppearance = new Label();
        private ComboBox cmbAppearance = new ComboBox();

        // Keeps enough state to describe an idle pause without making the form schedule anything.
        private BigDaddySchedulePhase currentPhase = BigDaddySchedulePhase.Playing;
        private bool playPausedForIdle;
        private TimeSpan remainingTime;
        private readonly bool startHidden;
        private bool isExiting;

        public BigDaddyForm(bool startHidden = false)
        {
            this.startHidden = startHidden;
            InitializeComponent();
            ConfigureBreakEnabledControls();
            ConfigureBreakMessageControls();
            ConfigureSecurityAndShutdownControls();
            ConfigurePlaySessionAssistanceControls();
            ConfigureWarningAndAppearanceControls();
            ConfigureIdleThresholdControls();
            ConfigureScheduleStatusLabel();
            ConfigureScheduleLifecycleControls();
            ConfigureResponsiveLayout();

            scheduler = new BigDaddyScheduler(settings);
            scheduler.TimeChanged += CountdownTimer_TimeChanged;
            scheduler.PhaseChanged += Scheduler_PhaseChanged;
            scheduler.PlayIdleStateChanged += Scheduler_PlayIdleStateChanged;
            scheduler.SessionStateChanged += Scheduler_SessionStateChanged;
            scheduler.LongBreakCompleted += Scheduler_LongBreakCompleted;
            breakOverlay.KeyboardUnlockRequested += BreakOverlay_KeyboardUnlockRequested;
            breakOverlay.ShutdownRequested += BreakOverlay_ShutdownRequested;
            breakOverlay.SignOutRequested += BreakOverlay_SignOutRequested;
            breakOverlay.SwitchUserRequested += BreakOverlay_SwitchUserRequested;
            notificationService.ShowRequested += NotificationService_ShowRequested;
            notificationService.ExitRequested += NotificationService_ExitRequested;
            FormClosing += BigDaddyForm_FormClosing;
            FormClosed += BigDaddyForm_FormClosed;
            Shown += BigDaddyForm_Shown;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            ApplyAppearance();

            if (startHidden)
            {
                ShowInTaskbar = false;
                Opacity = 0;
            }
        }

        // Displays the current phase's remaining time in HH:MM:SS format.
        private void CountdownTimer_TimeChanged(object? sender, TimeSpan remainingTime)
        {
            this.remainingTime = remainingTime;
            lblCountdown.Text = remainingTime.ToString(@"hh\:mm\:ss");
            breakOverlay.UpdateCountdown(remainingTime);
            UpdatePlaySessionOsd();
            UpdateTrayStatus();
        }

        // Updates the visible phase whenever the scheduler starts play or either type of break.
        private void Scheduler_PhaseChanged(object? sender, BigDaddySchedulePhase phase)
        {
            currentPhase = phase;
            UpdateScheduleStatus();
            UpdateScheduleControlState();

            if (phase == BigDaddySchedulePhase.Playing)
            {
                breakOverlay.HideBreak();
                UpdatePlaySessionOsd();
            }
            else if (phase == BigDaddySchedulePhase.ShortBreak)
            {
                TrySendEscapeAtBreakStart();
                UpdatePlaySessionOsd();
                breakOverlay.ShowBreak(
                    this,
                    "Short Break",
                    settings.ShortBreakMessage,
                    BigDaddyPinService.HasPin(settings),
                    remainingTime);

                if (settings.ShortBreakNotificationsEnabled)
                {
                    notificationService.ShowBreakStartedNotification("Short Break");
                }
            }
            else if (phase == BigDaddySchedulePhase.LongBreak)
            {
                TrySendEscapeAtBreakStart();
                UpdatePlaySessionOsd();
                breakOverlay.ShowBreak(
                    this,
                    "Long Break",
                    settings.LongBreakMessage,
                    BigDaddyPinService.HasPin(settings),
                    remainingTime);

                if (settings.LongBreakNotificationsEnabled)
                {
                    notificationService.ShowBreakStartedNotification("Long Break");
                }

                PersistSessionState();

                if (settings.AutomaticLongBreakShutdown && !scheduler.IsRestoringSession)
                {
                    BigDaddyShutdownService.TryShutdown();
                }
            }
        }

        // Saves each countdown update so an interruption cannot discard a break.
        private void Scheduler_SessionStateChanged(object? sender, BigDaddySessionState sessionState)
        {
            if (scheduler.IsRunning)
            {
                sessionStateStore.TrySave(sessionState);
            }
            else
            {
                sessionStateStore.TryClear();
            }
        }

        // Keeps a calendar-day total of completed long breaks without making the scheduler own settings storage.
        private void Scheduler_LongBreakCompleted(object? sender, DateTime completedAt)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            if (settings.LongBreakCountDate != today)
            {
                settings.LongBreakCountDate = today;
                settings.CompletedLongBreaksToday = 0;
            }

            if (DateOnly.FromDateTime(completedAt) == today)
            {
                settings.CompletedLongBreaksToday++;
            }

            settingsStore.TrySave(settings, out _);
            UpdatePlaySessionOsd();
        }

        // A new local calendar day starts with no completed long breaks in the OSD total.
        private void EnsureCurrentDayLongBreakCount()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            if (settings.LongBreakCountDate == today)
            {
                return;
            }

            settings.LongBreakCountDate = today;
            settings.CompletedLongBreaksToday = 0;
            settingsStore.TrySave(settings, out _);
        }

        private void PersistSessionState()
        {
            if (scheduler.IsRunning)
            {
                sessionStateStore.TrySave(scheduler.GetSessionState());
            }
            else
            {
                sessionStateStore.TryClear();
            }
        }

        private void BigDaddyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            PersistSessionState();

            if (!isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideMainWindow();
            }
        }

        // Cleans up the Windows notification-area icon when the form closes.
        private void BigDaddyForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            notificationService.ShowRequested -= NotificationService_ShowRequested;
            notificationService.ExitRequested -= NotificationService_ExitRequested;
            notificationService.Dispose();
            breakOverlay.Dispose();
            playSessionOsd.Dispose();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        // Keeps automatic Windows startup out of the taskbar without briefly showing the form.
        private void BigDaddyForm_Shown(object? sender, EventArgs e)
        {
            if (startHidden)
            {
                HideMainWindow();
                Opacity = 1;
            }
        }

        private void NotificationService_ShowRequested(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void NotificationService_ExitRequested(object? sender, EventArgs e)
        {
            isExiting = true;
            Application.Exit();
        }

        private void ShowMainWindow()
        {
            ShowInTaskbar = true;
            if (!Visible)
            {
                Show();
            }

            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void HideMainWindow()
        {
            Hide();
            ShowInTaskbar = false;
        }

        // Updates the visible message when playing freezes or resumes because the user is idle.
        private void Scheduler_PlayIdleStateChanged(object? sender, bool isPaused)
        {
            playPausedForIdle = isPaused;
            UpdateScheduleStatus();
        }

        // Creates the status label in code so the Visual Studio-generated Designer file stays untouched.
        private void ConfigureScheduleStatusLabel()
        {
            lblScheduleStatus.AutoSize = false;
            lblScheduleStatus.Name = "lblScheduleStatus";
            lblScheduleStatus.Size = new Size(260, 20);
            lblScheduleStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblScheduleStatus.Text = "Ready";
            Controls.Add(lblScheduleStatus);
        }

        // Adds explicit controls so validation never starts or resets a schedule by itself.
        private void ConfigureScheduleLifecycleControls()
        {
            btnValidate.Text = "Validate Settings";
            btnStart.Text = "Start";
            btnPause.Text = "Pause";
            btnStop.Text = "Stop";

            btnStart.Click += BtnStart_Click;
            btnPause.Click += BtnPause_Click;
            btnStop.Click += BtnStop_Click;

            Controls.Add(btnStart);
            Controls.Add(btnPause);
            Controls.Add(btnStop);
        }

        // Reflows both Designer-created and supplemental controls so they remain usable at
        // Windows scaling levels and on smaller displays.
        private void ConfigureResponsiveLayout()
        {
            AutoScroll = true;
            MinimumSize = new Size(440, 380);
            Resize += (_, _) => ArrangeResponsiveControls();
            ArrangeResponsiveControls();
        }

        private void ArrangeResponsiveControls()
        {
            int availableWidth = Math.Max(ClientSize.Width, 420);
            const int left = 24;
            const int fieldLeft = 235;
            const int rowHeight = 34;
            bool useWideLayout = availableWidth >= 760;
            int y = 20;

            if (useWideLayout)
            {
                ArrangeBreakControls(label1, txtShortBreakInterval, chkShortBreaksEnabled,
                    chkShortBreakNotificationsEnabled, btnShortBreakMessage, y, true);
                y += rowHeight;
                ArrangeBreakControls(label2, txtShortBreakDuration, null, null,
                    null, y, true);
                y += rowHeight;
                ArrangeBreakControls(label3, txtLongBreakInterval, chkLongBreaksEnabled,
                    chkLongBreakNotificationsEnabled, btnLongBreakMessage, y, true);
                y += rowHeight;
                ArrangeBreakControls(label4, txtLongBreakDuration, null, null,
                    null, y, true);
                y += 18;

                lblIdleThreshold.Location = new Point(left, y + 3);
                txtIdleThreshold.Location = new Point(fieldLeft, y);
                y += 38;

                chkAutomaticLongBreakShutdown.Location = new Point(left, y);
                btnSetEmergencyPin.Location = new Point(280, y - 3);
                btnClearEmergencyPin.Location = new Point(386, y - 3);
                y += 34;
                btnSignOut.Location = new Point(left, y);
                btnSwitchUser.Location = new Point(130, y);
                btnShutdown.Location = new Point(250, y);
                chkPlaySessionOsdEnabled.Location = new Point(370, y + 3);
                chkSendEscapeAtBreakStart.Location = new Point(490, y + 3);
                y += 38;

                lblFlashWarningStart.Location = new Point(left, y + 3);
                txtFlashWarningStart.Location = new Point(180, y);
                lblAppearance.Location = new Point(330, y + 3);
                cmbAppearance.Location = new Point(410, y);
                y += 36;
            }
            else
            {
                y = ArrangeNarrowBreakControls(label1, txtShortBreakInterval,
                    chkShortBreaksEnabled, chkShortBreakNotificationsEnabled,
                    btnShortBreakMessage, y);
                y = ArrangeNarrowBreakControls(label2, txtShortBreakDuration,
                    null, null, null, y);
                y += 10;
                y = ArrangeNarrowBreakControls(label3, txtLongBreakInterval,
                    chkLongBreaksEnabled, chkLongBreakNotificationsEnabled,
                    btnLongBreakMessage, y);
                y = ArrangeNarrowBreakControls(label4, txtLongBreakDuration,
                    null, null, null, y);
                y += 10;

                lblIdleThreshold.Location = new Point(left, y + 3);
                txtIdleThreshold.Location = new Point(fieldLeft, y);
                y += 38;

                chkAutomaticLongBreakShutdown.Location = new Point(left, y);
                y += 32;
                btnSetEmergencyPin.Location = new Point(left, y);
                btnClearEmergencyPin.Location = new Point(left + 106, y);
                y += 34;
                btnSignOut.Location = new Point(left, y);
                btnSwitchUser.Location = new Point(left + 110, y);
                btnShutdown.Location = new Point(left + 230, y);
                y += 32;
                chkPlaySessionOsdEnabled.Location = new Point(left, y + 2);
                chkSendEscapeAtBreakStart.Location = new Point(left + 120, y + 2);
                y += 34;

                lblFlashWarningStart.Location = new Point(left, y + 3);
                txtFlashWarningStart.Location = new Point(180, y);
                y += 32;
                lblAppearance.Location = new Point(left, y + 3);
                cmbAppearance.Location = new Point(180, y);
                y += 36;
            }

            lblScheduleStatus.Location = new Point(
                Math.Max(left, (availableWidth - lblScheduleStatus.Width) / 2), y);
            y += 28;
            lblCountdown.Location = new Point(
                Math.Max(left, (availableWidth - lblCountdown.Width) / 2), y);
            y += 30;
            btnValidate.Size = new Size(110, 28);
            btnStart.Size = new Size(70, 28);
            btnPause.Size = new Size(80, 28);
            btnStop.Size = new Size(70, 28);

            int lifecycleWidth = btnValidate.Width + btnStart.Width + btnPause.Width + btnStop.Width + 24;
            int lifecycleLeft = Math.Max(left, (availableWidth - lifecycleWidth) / 2);
            btnValidate.Location = new Point(lifecycleLeft, y);
            btnStart.Location = new Point(btnValidate.Right + 8, y);
            btnPause.Location = new Point(btnStart.Right + 8, y);
            btnStop.Location = new Point(btnPause.Right + 8, y);

            AutoScrollMinSize = new Size(0, y + btnValidate.Height + 30);
        }

        private static void ArrangeBreakControls(
            Label label,
            Control timeBox,
            CheckBox? enabledBox,
            CheckBox? notificationBox,
            Button? messageButton,
            int y,
            bool useWideLayout)
        {
            label.Location = new Point(24, y + 3);
            timeBox.Location = new Point(235, y);
            timeBox.Size = new Size(120, 23);

            if (useWideLayout && enabledBox != null)
            {
                enabledBox.Location = new Point(375, y + 3);
                notificationBox!.Location = new Point(465, y + 3);
                messageButton!.Location = new Point(555, y - 1);
                messageButton.Size = new Size(110, 27);
            }
        }

        private static int ArrangeNarrowBreakControls(
            Label label,
            Control timeBox,
            CheckBox? enabledBox,
            CheckBox? notificationBox,
            Button? messageButton,
            int y)
        {
            label.Location = new Point(24, y + 3);
            timeBox.Location = new Point(235, y);
            timeBox.Size = new Size(120, 23);
            y += 29;

            if (enabledBox != null)
            {
                enabledBox.Location = new Point(24, y + 2);
                notificationBox!.Location = new Point(120, y + 2);
                messageButton!.Location = new Point(210, y - 1);
                messageButton.Size = new Size(110, 27);
                y += 32;
            }

            return y;
        }

        // Adds break-type checkboxes without changing the generated Designer file.
        private void ConfigureBreakEnabledControls()
        {
            chkShortBreaksEnabled.AutoSize = true;
            chkShortBreaksEnabled.Checked = settings.ShortBreaksEnabled;
            chkShortBreaksEnabled.Location = new Point(540, 15);
            chkShortBreaksEnabled.Name = "chkShortBreaksEnabled";
            chkShortBreaksEnabled.Text = "Enabled";
            chkShortBreaksEnabled.CheckedChanged += BreaksEnabled_CheckedChanged;

            chkLongBreaksEnabled.AutoSize = true;
            chkLongBreaksEnabled.Checked = settings.LongBreaksEnabled;
            chkLongBreaksEnabled.Location = new Point(540, 123);
            chkLongBreaksEnabled.Name = "chkLongBreaksEnabled";
            chkLongBreaksEnabled.Text = "Enabled";
            chkLongBreaksEnabled.CheckedChanged += BreaksEnabled_CheckedChanged;

            chkShortBreakNotificationsEnabled.AutoSize = true;
            chkShortBreakNotificationsEnabled.Checked = settings.ShortBreakNotificationsEnabled;
            chkShortBreakNotificationsEnabled.Location = new Point(630, 15);
            chkShortBreakNotificationsEnabled.Name = "chkShortBreakNotificationsEnabled";
            chkShortBreakNotificationsEnabled.Text = "Notify";

            chkLongBreakNotificationsEnabled.AutoSize = true;
            chkLongBreakNotificationsEnabled.Checked = settings.LongBreakNotificationsEnabled;
            chkLongBreakNotificationsEnabled.Location = new Point(630, 123);
            chkLongBreakNotificationsEnabled.Name = "chkLongBreakNotificationsEnabled";
            chkLongBreakNotificationsEnabled.Text = "Notify";

            Controls.Add(chkShortBreaksEnabled);
            Controls.Add(chkLongBreaksEnabled);
            Controls.Add(chkShortBreakNotificationsEnabled);
            Controls.Add(chkLongBreakNotificationsEnabled);
            UpdateBreakControlState();
        }

        // Adds separate dialogs for the optional short- and long-break overlay messages.
        private void ConfigureBreakMessageControls()
        {
            btnShortBreakMessage.Location = new Point(630, 45);
            btnShortBreakMessage.Size = new Size(110, 23);
            btnShortBreakMessage.Text = "Message...";
            btnShortBreakMessage.Click += BtnShortBreakMessage_Click;

            btnLongBreakMessage.Location = new Point(630, 153);
            btnLongBreakMessage.Size = new Size(110, 23);
            btnLongBreakMessage.Text = "Message...";
            btnLongBreakMessage.Click += BtnLongBreakMessage_Click;

            Controls.Add(btnShortBreakMessage);
            Controls.Add(btnLongBreakMessage);
        }

        // Adds the optional long-break shutdown choice, emergency PIN controls, and manual shutdown.
        private void ConfigureSecurityAndShutdownControls()
        {
            chkAutomaticLongBreakShutdown.AutoSize = true;
            chkAutomaticLongBreakShutdown.Location = new Point(540, 181);
            chkAutomaticLongBreakShutdown.Text = "Shut down at long break";

            btnSetEmergencyPin.Location = new Point(540, 210);
            btnSetEmergencyPin.Size = new Size(100, 23);
            btnSetEmergencyPin.Text = "Set PIN...";
            btnSetEmergencyPin.Click += BtnSetEmergencyPin_Click;

            btnClearEmergencyPin.Location = new Point(646, 210);
            btnClearEmergencyPin.Size = new Size(94, 23);
            btnClearEmergencyPin.Text = "Clear PIN";
            btnClearEmergencyPin.Click += BtnClearEmergencyPin_Click;

            btnShutdown.Location = new Point(646, 240);
            btnShutdown.Size = new Size(94, 23);
            btnShutdown.Text = "Shut Down...";
            btnShutdown.Click += BtnShutdown_Click;

            btnSignOut.Size = new Size(100, 23);
            btnSignOut.Text = "Sign Out...";
            btnSignOut.Click += BtnSignOut_Click;

            btnSwitchUser.Size = new Size(110, 23);
            btnSwitchUser.Text = "Switch User...";
            btnSwitchUser.Click += BtnSwitchUser_Click;

            Controls.Add(chkAutomaticLongBreakShutdown);
            Controls.Add(btnSetEmergencyPin);
            Controls.Add(btnClearEmergencyPin);
            Controls.Add(btnShutdown);
            Controls.Add(btnSignOut);
            Controls.Add(btnSwitchUser);
            UpdateSecurityControls();
        }

        // Adds the two small opt-in options for a play countdown and a best-effort game pause.
        private void ConfigurePlaySessionAssistanceControls()
        {
            chkPlaySessionOsdEnabled.AutoSize = true;
            chkPlaySessionOsdEnabled.Text = "Show play OSD";
            chkPlaySessionOsdEnabled.Checked = settings.PlaySessionOsdEnabled;
            chkPlaySessionOsdEnabled.CheckedChanged += PlaySessionOsdEnabled_CheckedChanged;

            chkSendEscapeAtBreakStart.AutoSize = true;
            chkSendEscapeAtBreakStart.Text = "Attempt Escape at break start";
            chkSendEscapeAtBreakStart.Checked = settings.SendEscapeAtBreakStart;

            Controls.Add(chkPlaySessionOsdEnabled);
            Controls.Add(chkSendEscapeAtBreakStart);
        }

        // Adds the two preferences that customize the existing visual warning without changing timing.
        private void ConfigureWarningAndAppearanceControls()
        {
            lblFlashWarningStart.AutoSize = true;
            lblFlashWarningStart.Text = "Flash warning starts at:";
            txtFlashWarningStart.Size = new Size(120, 23);
            txtFlashWarningStart.Text = settings.FlashWarningStart.ToString(@"hh\:mm\:ss");

            lblAppearance.AutoSize = true;
            lblAppearance.Text = "Appearance:";
            cmbAppearance.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAppearance.Items.AddRange(new object[] { "Follow Windows", "Light", "Dark neon" });
            cmbAppearance.SelectedIndex = (int)settings.Appearance;
            cmbAppearance.Size = new Size(130, 23);
            cmbAppearance.SelectedIndexChanged += Appearance_SelectedIndexChanged;

            Controls.Add(lblFlashWarningStart);
            Controls.Add(txtFlashWarningStart);
            Controls.Add(lblAppearance);
            Controls.Add(cmbAppearance);
        }

        private void BtnSetEmergencyPin_Click(object? sender, EventArgs e)
        {
            using BigDaddyPinDialog firstEntry = new BigDaddyPinDialog(
                "Set Emergency PIN", "Enter a numeric emergency PIN using the keypad.");

            if (firstEntry.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(firstEntry.Pin))
            {
                return;
            }

            using BigDaddyPinDialog confirmationEntry = new BigDaddyPinDialog(
                "Confirm Emergency PIN", "Enter the same numeric PIN again.");

            if (confirmationEntry.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (firstEntry.Pin != confirmationEntry.Pin)
            {
                MessageBox.Show("The PIN entries do not match.");
                return;
            }

            BigDaddyPinService.SetPin(settings, firstEntry.Pin);
            SaveCurrentSettingsIfValid();
            UpdateSecurityControls();
        }

        private void BtnClearEmergencyPin_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Remove the emergency PIN and leave keyboard input available during breaks?",
                    "Clear Emergency PIN",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            BigDaddyPinService.ClearPin(settings);
            SaveCurrentSettingsIfValid();
            UpdateSecurityControls();
        }

        private void BtnShutdown_Click(object? sender, EventArgs e)
        {
            RequestShutdown();
        }

        private void BreakOverlay_ShutdownRequested(object? sender, EventArgs e)
        {
            RequestShutdown();
        }

        private void BtnSignOut_Click(object? sender, EventArgs e)
        {
            RequestSignOut();
        }

        private void BreakOverlay_SignOutRequested(object? sender, EventArgs e)
        {
            RequestSignOut();
        }

        private void BtnSwitchUser_Click(object? sender, EventArgs e)
        {
            RequestSwitchUser();
        }

        private void BreakOverlay_SwitchUserRequested(object? sender, EventArgs e)
        {
            RequestSwitchUser();
        }

        private void RequestShutdown()
        {
            IWin32Window owner = breakOverlay.Visible ? breakOverlay : this;

            if (MessageBox.Show(
                    owner,
                    "Shut down this PC now?",
                    "Big Daddy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            PersistSessionState();

            if (!BigDaddyShutdownService.TryShutdown())
            {
                MessageBox.Show("Windows could not start the shutdown.");
            }
        }

        private void RequestSignOut()
        {
            IWin32Window owner = breakOverlay.Visible ? breakOverlay : this;

            if (MessageBox.Show(owner, "Sign out the current Windows user now?", "Big Daddy",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            PersistSessionState();
            if (!BigDaddyWindowsSessionService.TrySignOut())
            {
                MessageBox.Show(owner, "Windows could not sign out the current user.", "Big Daddy");
            }
        }

        private void RequestSwitchUser()
        {
            IWin32Window owner = breakOverlay.Visible ? breakOverlay : this;

            if (MessageBox.Show(owner, "Switch to the Windows sign-in screen now?", "Big Daddy",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            PersistSessionState();
            if (!BigDaddyWindowsSessionService.TrySwitchUser())
            {
                MessageBox.Show(owner, "Windows could not switch the current user session.", "Big Daddy");
            }
        }

        private void PlaySessionOsdEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            UpdatePlaySessionOsd();
        }

        private void Appearance_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyAppearance();
        }

        // Refreshes Follow Windows if Windows changes its light/dark preference while Big Daddy runs.
        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (cmbAppearance.SelectedIndex == (int)BigDaddyAppearance.FollowWindows &&
                IsHandleCreated && !IsDisposed)
            {
                BeginInvoke((MethodInvoker)ApplyAppearance);
            }
        }

        private void ApplyAppearance()
        {
            BigDaddyAppearance appearance = cmbAppearance.SelectedIndex >= 0
                ? (BigDaddyAppearance)cmbAppearance.SelectedIndex
                : BigDaddyAppearance.FollowWindows;
            bool useDarkTheme = appearance == BigDaddyAppearance.DarkNeon ||
                appearance == BigDaddyAppearance.FollowWindows && WindowsPrefersDarkTheme();
            bool useNeonTheme = appearance == BigDaddyAppearance.DarkNeon;

            Color surface = useNeonTheme ? Color.FromArgb(14, 21, 27) :
                useDarkTheme ? Color.FromArgb(28, 31, 36) : SystemColors.Control;
            Color controlSurface = useNeonTheme ? Color.FromArgb(22, 42, 49) :
                useDarkTheme ? Color.FromArgb(43, 48, 56) : SystemColors.Window;
            Color textColor = useNeonTheme ? Color.FromArgb(211, 255, 248) :
                useDarkTheme ? Color.FromArgb(238, 242, 245) : SystemColors.ControlText;
            Color accent = useNeonTheme ? Color.FromArgb(11, 114, 128) :
                useDarkTheme ? Color.FromArgb(43, 100, 108) : SystemColors.Control;

            BackColor = surface;
            ForeColor = textColor;
            ApplyControlTheme(this, surface, controlSurface, textColor, accent, useDarkTheme, useNeonTheme);
            breakOverlay.ApplyTheme(useDarkTheme);
            playSessionOsd.ApplyTheme(useDarkTheme, useNeonTheme);
            UpdatePlaySessionOsd();
        }

        private static void ApplyControlTheme(
            Control parent,
            Color surface,
            Color controlSurface,
            Color textColor,
            Color accent,
            bool useDarkTheme,
            bool useNeonTheme)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button)
                {
                    button.UseVisualStyleBackColor = false;
                    button.FlatStyle = useDarkTheme ? FlatStyle.Flat : FlatStyle.Standard;
                    button.BackColor = accent;
                    button.ForeColor = textColor;
                    button.FlatAppearance.BorderColor = useNeonTheme
                        ? Color.FromArgb(74, 229, 210)
                        : useDarkTheme ? Color.FromArgb(93, 141, 149) : SystemColors.ControlDark;
                    button.FlatAppearance.MouseOverBackColor = useNeonTheme
                        ? Color.FromArgb(14, 143, 153)
                        : accent;
                }
                else if (control is TextBoxBase or ComboBox)
                {
                    control.BackColor = controlSurface;
                    control.ForeColor = textColor;
                }
                else
                {
                    control.BackColor = surface;
                    control.ForeColor = textColor;
                }

                if (control.HasChildren)
                {
                    ApplyControlTheme(control, surface, controlSurface, textColor, accent, useDarkTheme, useNeonTheme);
                }
            }
        }

        private static bool WindowsPrefersDarkTheme()
        {
            try
            {
                object? value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    1);
                return value is int lightThemeEnabled && lightThemeEnabled == 0;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
        }

        // The warning alternates colour on each second after the chosen threshold is reached.
        private void UpdatePlaySessionOsd()
        {
            BigDaddyScheduler? activeScheduler = scheduler;
            if (chkPlaySessionOsdEnabled.Checked && activeScheduler is not null && activeScheduler.IsRunning &&
                currentPhase == BigDaddySchedulePhase.Playing)
            {
                bool warning = remainingTime > TimeSpan.Zero && remainingTime <= settings.FlashWarningStart;
                playSessionOsd.ShowPlayTime(
                    remainingTime,
                    warning,
                    activeScheduler.CurrentPlaySessionNumber,
                    activeScheduler.SessionsPerLongBreak,
                    settings.CompletedLongBreaksToday);
            }
            else
            {
                playSessionOsd.HideOsd();
            }
        }

        // Escape is deliberately best-effort: the foreground program may ignore it or run elevated.
        private void TrySendEscapeAtBreakStart()
        {
            if (settings.SendEscapeAtBreakStart)
            {
                BigDaddyEscapeService.TrySendEscape();
            }
        }

        private void UpdateSecurityControls()
        {
            btnClearEmergencyPin.Enabled = BigDaddyPinService.HasPin(settings);
        }

        // Handles the mouse-driven keypad request without dismissing an active break.
        private void BreakOverlay_KeyboardUnlockRequested(object? sender, EventArgs e)
        {
            using BigDaddyPinDialog pinDialog = new BigDaddyPinDialog(
                "Emergency Keyboard Access", "Enter the emergency PIN using the keypad.");

            if (pinDialog.ShowDialog(breakOverlay) != DialogResult.OK)
            {
                return;
            }

            if (!BigDaddyPinService.VerifyPin(settings, pinDialog.Pin))
            {
                MessageBox.Show(breakOverlay, "The PIN is incorrect.", "Big Daddy");
                return;
            }

            breakOverlay.UnlockKeyboard();
        }

        private void BtnShortBreakMessage_Click(object? sender, EventArgs e)
        {
            EditBreakMessage("Short Break", settings.ShortBreakMessage,
                message => settings.ShortBreakMessage = message);
        }

        private void BtnLongBreakMessage_Click(object? sender, EventArgs e)
        {
            EditBreakMessage("Long Break", settings.LongBreakMessage,
                message => settings.LongBreakMessage = message);
        }

        // Applies the selected message immediately and saves it when the current settings are valid.
        private void EditBreakMessage(string breakName, string currentMessage, Action<string> saveMessage)
        {
            using BigDaddyBreakMessageDialog dialog =
                new BigDaddyBreakMessageDialog(breakName, currentMessage);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                saveMessage(dialog.MessageText);
                SaveCurrentSettingsIfValid();
            }
        }

        private void SaveCurrentSettingsIfValid()
        {
            if (settings.TryValidate(out _) &&
                !settingsStore.TrySave(settings, out string saveErrorMessage))
            {
                MessageBox.Show(saveErrorMessage);
            }
        }

        // Enables or disables each interval and duration pair to match its checkbox.
        private void BreaksEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateBreakControlState();
        }

        private void UpdateBreakControlState()
        {
            txtShortBreakInterval.Enabled = chkShortBreaksEnabled.Checked;
            txtShortBreakDuration.Enabled = chkShortBreaksEnabled.Checked;
            label1.Enabled = chkShortBreaksEnabled.Checked;
            label2.Enabled = chkShortBreaksEnabled.Checked;
            chkShortBreakNotificationsEnabled.Enabled = chkShortBreaksEnabled.Checked;
            btnShortBreakMessage.Enabled = chkShortBreaksEnabled.Checked;

            txtLongBreakInterval.Enabled = chkLongBreaksEnabled.Checked;
            txtLongBreakDuration.Enabled = chkLongBreaksEnabled.Checked;
            label3.Enabled = chkLongBreaksEnabled.Checked;
            label4.Enabled = chkLongBreaksEnabled.Checked;
            chkLongBreakNotificationsEnabled.Enabled = chkLongBreaksEnabled.Checked;
            btnLongBreakMessage.Enabled = chkLongBreaksEnabled.Checked;
            chkAutomaticLongBreakShutdown.Enabled = chkLongBreaksEnabled.Checked;
        }

        // Adds the configurable idle-time field without changing the generated Designer file.
        private void ConfigureIdleThresholdControls()
        {
            lblIdleThreshold.AutoSize = true;
            lblIdleThreshold.Location = new Point(99, 218);
            lblIdleThreshold.Name = "lblIdleThreshold";
            lblIdleThreshold.Text = "Idle Time Before Pause";

            txtIdleThreshold.Location = new Point(421, 215);
            txtIdleThreshold.Name = "txtIdleThreshold";
            txtIdleThreshold.Size = new Size(100, 23);
            txtIdleThreshold.Text = settings.IdleThreshold.ToString(@"hh\:mm\:ss");

            Controls.Add(lblIdleThreshold);
            Controls.Add(txtIdleThreshold);
        }

        // Converts scheduler state into the short plain-English message shown to the user.
        private void UpdateScheduleStatus()
        {
            if (scheduler.IsManuallyPaused)
            {
                lblScheduleStatus.Text = currentPhase == BigDaddySchedulePhase.ShortBreak
                    ? "Short Break - manually paused"
                    : currentPhase == BigDaddySchedulePhase.LongBreak
                        ? "Long Break - manually paused"
                        : "Playing - manually paused";
            }
            else if (currentPhase == BigDaddySchedulePhase.ShortBreak)
            {
                lblScheduleStatus.Text = "Short Break";
            }
            else if (currentPhase == BigDaddySchedulePhase.LongBreak)
            {
                lblScheduleStatus.Text = "Long Break";
            }
            else if (playPausedForIdle)
            {
                lblScheduleStatus.Text = "Playing - paused while idle";
            }
            else
            {
                lblScheduleStatus.Text = "Playing";
            }

            UpdateTrayStatus();
        }

        private void UpdateScheduleControlState()
        {
            btnPause.Enabled = scheduler.IsRunning;
            btnPause.Text = scheduler.IsManuallyPaused ? "Resume" : "Pause";
            btnStop.Enabled = scheduler.IsRunning;
        }

        private void UpdateTrayStatus()
        {
            notificationService.UpdateStatus(lblScheduleStatus.Text, remainingTime);
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            TryValidateAndSaveSettings(showSuccessMessage: true);
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (!TryValidateAndSaveSettings(showSuccessMessage: false))
            {
                return;
            }

            if (scheduler.IsRunning)
            {
                DialogResult choice = MessageBox.Show(
                    "A schedule is already in progress.\n\n" +
                    "Yes: continue the current session.\n" +
                    "No: start a new session from the beginning.\n" +
                    "Cancel: make no change.",
                    "Start Big Daddy",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    if (scheduler.IsManuallyPaused)
                    {
                        scheduler.Resume();
                    }
                }
                else if (choice == DialogResult.No)
                {
                    scheduler.Start();
                }
                else
                {
                    return;
                }
            }
            else
            {
                scheduler.Start();
            }

            UpdateScheduleStatus();
            UpdateScheduleControlState();
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            if (scheduler.IsManuallyPaused)
            {
                scheduler.Resume();
            }
            else
            {
                scheduler.Pause();
            }

            UpdateScheduleStatus();
            UpdateScheduleControlState();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Stop the current schedule? Its recoverable session state will be cleared.",
                    "Stop Big Daddy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            scheduler.Stop();
            breakOverlay.HideBreak();
            sessionStateStore.TryClear();
            remainingTime = TimeSpan.Zero;
            lblCountdown.Text = remainingTime.ToString(@"hh\:mm\:ss");
            UpdatePlaySessionOsd();
            lblScheduleStatus.Text = "Stopped";
            UpdateTrayStatus();
            UpdateScheduleControlState();
        }

        // Validates and saves settings only. It deliberately does not change the live schedule.
        private bool TryValidateAndSaveSettings(bool showSuccessMessage)
        {
            if (!TimeSpan.TryParse(
                    txtShortBreakInterval.Text,
                    out TimeSpan shortBreakInterval) ||
                shortBreakInterval <= TimeSpan.Zero)
            {
                MessageBox.Show("Short Break Interval must be in HH:MM:SS format and greater than zero.");
                return false;
            }

            if (!TimeSpan.TryParse(
                    txtShortBreakDuration.Text,
                    out TimeSpan shortBreakDuration) ||
                shortBreakDuration <= TimeSpan.Zero)
            {
                MessageBox.Show("Short Break Duration must be in HH:MM:SS format and greater than zero.");
                return false;
            }

            if (!TimeSpan.TryParse(
                    txtLongBreakInterval.Text,
                    out TimeSpan longBreakInterval) ||
                longBreakInterval <= TimeSpan.Zero)
            {
                MessageBox.Show("Long Break Interval must be in HH:MM:SS format and greater than zero.");
                return false;
            }

            if (!TimeSpan.TryParse(
                    txtLongBreakDuration.Text,
                    out TimeSpan longBreakDuration) ||
                longBreakDuration <= TimeSpan.Zero)
            {
                MessageBox.Show("Long Break Duration must be in HH:MM:SS format and greater than zero.");
                return false;
            }

            if (!TimeSpan.TryParse(
                    txtIdleThreshold.Text,
                    out TimeSpan idleThreshold) ||
                idleThreshold <= TimeSpan.Zero)
            {
                MessageBox.Show("Idle Time Before Pause must be in HH:MM:SS format and greater than zero.");
                return false;
            }

            if (!TimeSpan.TryParse(
                    txtFlashWarningStart.Text,
                    out TimeSpan flashWarningStart) ||
                flashWarningStart < TimeSpan.FromSeconds(1))
            {
                MessageBox.Show("Flash Warning Starts At must be in HH:MM:SS format and at least one second.");
                return false;
            }

            BigDaddySettings proposedSettings = new BigDaddySettings
            {
                ShortBreakInterval = shortBreakInterval,
                ShortBreakDuration = shortBreakDuration,
                LongBreakInterval = longBreakInterval,
                LongBreakDuration = longBreakDuration,
                IdleThreshold = idleThreshold,
                ShortBreaksEnabled = chkShortBreaksEnabled.Checked,
                LongBreaksEnabled = chkLongBreaksEnabled.Checked,
                ShortBreakNotificationsEnabled = chkShortBreakNotificationsEnabled.Checked,
                LongBreakNotificationsEnabled = chkLongBreakNotificationsEnabled.Checked,
                ShortBreakMessage = settings.ShortBreakMessage,
                LongBreakMessage = settings.LongBreakMessage,
                EmergencyPinHash = settings.EmergencyPinHash,
                EmergencyPinSalt = settings.EmergencyPinSalt,
                AutomaticLongBreakShutdown = chkAutomaticLongBreakShutdown.Checked,
                PlaySessionOsdEnabled = chkPlaySessionOsdEnabled.Checked,
                SendEscapeAtBreakStart = chkSendEscapeAtBreakStart.Checked,
                FlashWarningStart = flashWarningStart,
                Appearance = (BigDaddyAppearance)cmbAppearance.SelectedIndex,
                LongBreakCountDate = settings.LongBreakCountDate,
                CompletedLongBreaksToday = settings.CompletedLongBreaksToday
            };

            if (!proposedSettings.TryValidate(out string validationMessage))
            {
                MessageBox.Show(validationMessage);
                return false;
            }

            settings.ShortBreakInterval = proposedSettings.ShortBreakInterval;
            settings.ShortBreakDuration = proposedSettings.ShortBreakDuration;
            settings.LongBreakInterval = proposedSettings.LongBreakInterval;
            settings.LongBreakDuration = proposedSettings.LongBreakDuration;
            settings.IdleThreshold = proposedSettings.IdleThreshold;
            settings.ShortBreaksEnabled = proposedSettings.ShortBreaksEnabled;
            settings.LongBreaksEnabled = proposedSettings.LongBreaksEnabled;
            settings.ShortBreakNotificationsEnabled = proposedSettings.ShortBreakNotificationsEnabled;
            settings.LongBreakNotificationsEnabled = proposedSettings.LongBreakNotificationsEnabled;
            settings.ShortBreakMessage = proposedSettings.ShortBreakMessage;
            settings.LongBreakMessage = proposedSettings.LongBreakMessage;
            settings.EmergencyPinHash = proposedSettings.EmergencyPinHash;
            settings.EmergencyPinSalt = proposedSettings.EmergencyPinSalt;
            settings.AutomaticLongBreakShutdown = proposedSettings.AutomaticLongBreakShutdown;
            settings.PlaySessionOsdEnabled = proposedSettings.PlaySessionOsdEnabled;
            settings.SendEscapeAtBreakStart = proposedSettings.SendEscapeAtBreakStart;
            settings.FlashWarningStart = proposedSettings.FlashWarningStart;
            settings.Appearance = proposedSettings.Appearance;
            settings.LongBreakCountDate = proposedSettings.LongBreakCountDate;
            settings.CompletedLongBreaksToday = proposedSettings.CompletedLongBreaksToday;

            if (!settingsStore.TrySave(settings, out string saveErrorMessage))
            {
                MessageBox.Show(saveErrorMessage);
                return false;
            }
            else if (showSuccessMessage)
            {
                MessageBox.Show("All settings are valid and saved.");
            }

            return true;
        }

        private void txtShortBreakInterval_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblCountdown_Click(object sender, EventArgs e)
        {

        }

        // Restores the last valid settings when the form first opens.
        private void BigDaddyForm_Load(object sender, EventArgs e)
        {
            if (settingsStore.TryLoad(out BigDaddySettings savedSettings))
            {
                settings.ShortBreakInterval = savedSettings.ShortBreakInterval;
                settings.ShortBreakDuration = savedSettings.ShortBreakDuration;
                settings.LongBreakInterval = savedSettings.LongBreakInterval;
                settings.LongBreakDuration = savedSettings.LongBreakDuration;
                settings.IdleThreshold = savedSettings.IdleThreshold;
                settings.ShortBreaksEnabled = savedSettings.ShortBreaksEnabled;
                settings.LongBreaksEnabled = savedSettings.LongBreaksEnabled;
                settings.ShortBreakNotificationsEnabled = savedSettings.ShortBreakNotificationsEnabled;
                settings.LongBreakNotificationsEnabled = savedSettings.LongBreakNotificationsEnabled;
                settings.ShortBreakMessage = savedSettings.ShortBreakMessage;
                settings.LongBreakMessage = savedSettings.LongBreakMessage;
                settings.EmergencyPinHash = savedSettings.EmergencyPinHash;
                settings.EmergencyPinSalt = savedSettings.EmergencyPinSalt;
                settings.AutomaticLongBreakShutdown = savedSettings.AutomaticLongBreakShutdown;
                settings.PlaySessionOsdEnabled = savedSettings.PlaySessionOsdEnabled;
                settings.SendEscapeAtBreakStart = savedSettings.SendEscapeAtBreakStart;
                settings.FlashWarningStart = savedSettings.FlashWarningStart;
                settings.Appearance = savedSettings.Appearance;
                settings.LongBreakCountDate = savedSettings.LongBreakCountDate;
                settings.CompletedLongBreaksToday = savedSettings.CompletedLongBreaksToday;

                txtShortBreakInterval.Text = settings.ShortBreakInterval.ToString(@"hh\:mm\:ss");
                txtShortBreakDuration.Text = settings.ShortBreakDuration.ToString(@"hh\:mm\:ss");
                txtLongBreakInterval.Text = settings.LongBreakInterval.ToString(@"hh\:mm\:ss");
                txtLongBreakDuration.Text = settings.LongBreakDuration.ToString(@"hh\:mm\:ss");
                txtIdleThreshold.Text = settings.IdleThreshold.ToString(@"hh\:mm\:ss");
                chkShortBreaksEnabled.Checked = settings.ShortBreaksEnabled;
                chkLongBreaksEnabled.Checked = settings.LongBreaksEnabled;
                chkShortBreakNotificationsEnabled.Checked = settings.ShortBreakNotificationsEnabled;
                chkLongBreakNotificationsEnabled.Checked = settings.LongBreakNotificationsEnabled;
                chkAutomaticLongBreakShutdown.Checked = settings.AutomaticLongBreakShutdown;
                chkPlaySessionOsdEnabled.Checked = settings.PlaySessionOsdEnabled;
                chkSendEscapeAtBreakStart.Checked = settings.SendEscapeAtBreakStart;
                txtFlashWarningStart.Text = settings.FlashWarningStart.ToString(@"hh\:mm\:ss");
                cmbAppearance.SelectedIndex = (int)settings.Appearance;
            }

            UpdateBreakControlState();
            UpdateSecurityControls();
            EnsureCurrentDayLongBreakCount();
            ArrangeResponsiveControls();
            UpdateTrayStatus();
            UpdatePlaySessionOsd();
            ApplyAppearance();
            BigDaddyAutoStartService.TryEnable();

            if (settings.TryValidate(out _) && sessionStateStore.TryLoad(out BigDaddySessionState sessionState))
            {
                scheduler.Resume(sessionState);
            }

            UpdateScheduleControlState();
        }
    }
}
