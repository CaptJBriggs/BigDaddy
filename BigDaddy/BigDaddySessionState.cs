namespace BigDaddy
{
    // Records enough live scheduler state to resume safely after the app or PC closes.
    public class BigDaddySessionState
    {
        public BigDaddySchedulePhase Phase { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public TimeSpan PlayedTime { get; set; }
        public DateTime? BreakEndsAtUtc { get; set; }
        public bool IsManuallyPaused { get; set; }
    }
}
