namespace BigDaddy
{
    // Holds the four time values that control the break schedule.
    public class BigDaddySettings
    {
        // Length of one play session before a short break can begin.
        public TimeSpan ShortBreakInterval { get; set; }
        // Length of each short break.
        public TimeSpan ShortBreakDuration { get; set; }
        // Total active play time required before a long break begins.
        public TimeSpan LongBreakInterval { get; set; }
        // Length of each long break.
        public TimeSpan LongBreakDuration { get; set; }

        // Inactivity allowed before a play countdown freezes. Two minutes is the normal default.
        public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromMinutes(2);

        // Allows the user to temporarily skip short breaks while retaining their configured times.
        public bool ShortBreaksEnabled { get; set; } = true;

        // Allows the user to temporarily skip long breaks while retaining their configured times.
        public bool LongBreaksEnabled { get; set; } = true;

        // Controls whether Windows notifies the user when a short break begins.
        public bool ShortBreakNotificationsEnabled { get; set; } = true;

        // Controls whether Windows notifies the user when a long break begins.
        public bool LongBreakNotificationsEnabled { get; set; } = true;

        // Optional text shown on the short-break overlay; blank uses the default message.
        public string ShortBreakMessage { get; set; } = string.Empty;

        // Optional text shown on the long-break overlay; blank uses the default message.
        public string LongBreakMessage { get; set; } = string.Empty;

        // Salted hash data for the optional emergency numeric PIN; the PIN itself is never saved.
        public string EmergencyPinHash { get; set; } = string.Empty;
        public string EmergencyPinSalt { get; set; } = string.Empty;

        // Shuts down Windows at the beginning of an enabled long break when selected.
        public bool AutomaticLongBreakShutdown { get; set; }

        // Shows a small top-left countdown while the current play session is active.
        public bool PlaySessionOsdEnabled { get; set; }

        // Sends Escape immediately before a break overlay is displayed, when the user opts in.
        public bool SendEscapeAtBreakStart { get; set; }

        // Remaining play time at which the corner timer starts its visual warning.
        public TimeSpan FlashWarningStart { get; set; } = TimeSpan.FromSeconds(10);

        // The selected visual appearance; FollowWindows is the default for new and existing settings.
        public BigDaddyAppearance Appearance { get; set; } = BigDaddyAppearance.FollowWindows;

        // The local calendar date represented by CompletedLongBreaksToday.
        public DateOnly LongBreakCountDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        // Completed long breaks recorded for LongBreakCountDate.
        public int CompletedLongBreaksToday { get; set; }

        // Checks that the settings make sense together and returns a plain-English error if not.
        public bool TryValidate(out string validationMessage)
        {
            if (ShortBreakInterval <= TimeSpan.Zero ||
                ShortBreakDuration <= TimeSpan.Zero ||
                LongBreakInterval <= TimeSpan.Zero ||
                LongBreakDuration <= TimeSpan.Zero ||
                IdleThreshold <= TimeSpan.Zero ||
                FlashWarningStart < TimeSpan.FromSeconds(1))
            {
                validationMessage = "All time settings must be greater than zero. Flash Warning Starts At must be at least one second.";
                return false;
            }

            if (FlashWarningStart > ShortBreakInterval)
            {
                validationMessage = "Flash Warning Starts At cannot be longer than Short Break Interval.";
                return false;
            }

            if (LongBreakInterval <= ShortBreakInterval)
            {
                validationMessage = "Long Break Interval must be greater than Short Break Interval.";
                return false;
            }

            if (LongBreakInterval.Ticks % ShortBreakInterval.Ticks != 0)
            {
                validationMessage = "Long Break Interval must be an exact multiple of Short Break Interval.";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }
    }
}
