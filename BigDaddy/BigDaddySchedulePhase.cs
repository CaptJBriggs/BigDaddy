namespace BigDaddy
{
    // Names the three real phases of a Big Daddy schedule.
    // Idle is not a phase because it can happen only while playing.
    public enum BigDaddySchedulePhase
    {
        Playing,
        ShortBreak,
        LongBreak
    }
}
