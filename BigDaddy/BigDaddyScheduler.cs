namespace BigDaddy
{
    // Decides whether Big Daddy is in play, a short break, or a long break.
    internal class BigDaddyScheduler
    {
        // Validated configuration supplied by the form.
        private BigDaddySettings settings;
        // Performs the second-by-second countdown for the current phase.
        private BigDaddyTimer timer;
        // Detects whether the user is currently active at the computer.
        private BigDaddyIdleMonitor idleMonitor;
        // True during either type of break; false during play.
        private bool isBreak;
        // Remembers whether a play countdown is frozen specifically because of idle time.
        private bool playTimerPausedForIdle;
        // Total active play time accumulated since the previous long break.
        private TimeSpan playedTime;

        // The current schedule phase reported to the user interface.
        private BigDaddySchedulePhase? currentPhase;
        private bool isRestoringSession;
        private bool isRunning;
        private bool isManuallyPaused;

        public event EventHandler<TimeSpan>? TimeChanged;

        // Tells the form whenever play, short break, or long break begins.
        public event EventHandler<BigDaddySchedulePhase>? PhaseChanged;

        // Tells the form when play freezes or resumes because of idle time.
        public event EventHandler<bool>? PlayIdleStateChanged;

        // Supplies a complete snapshot whenever the live schedule changes.
        public event EventHandler<BigDaddySessionState>? SessionStateChanged;

        // Reports a completed long break so the form can update its saved daily total.
        public event EventHandler<DateTime>? LongBreakCompleted;

        // True only while a persisted phase is being raised during startup recovery.
        public bool IsRestoringSession => isRestoringSession;
        public bool IsRunning => isRunning;
        public bool IsManuallyPaused => isManuallyPaused;
        public int SessionsPerLongBreak => settings.ShortBreakInterval > TimeSpan.Zero
            ? (int)(settings.LongBreakInterval.Ticks / settings.ShortBreakInterval.Ticks)
            : 0;
        public int CurrentPlaySessionNumber => Math.Clamp(
            settings.ShortBreakInterval > TimeSpan.Zero
                ? (int)(playedTime.Ticks / settings.ShortBreakInterval.Ticks) + 1
                : 0,
            0,
            Math.Max(1, SessionsPerLongBreak));

        public BigDaddyScheduler(BigDaddySettings settings)
        {
            this.settings = settings;

            timer = new BigDaddyTimer();
            timer.TimeChanged += Timer_TimeChanged;

            idleMonitor = new BigDaddyIdleMonitor(TimeSpan.FromMinutes(2));
            idleMonitor.IdleStateChanged += IdleMonitor_IdleStateChanged;
        }

        // Forwards countdown updates and starts the next phase when a countdown reaches zero.
        private void Timer_TimeChanged(object? sender, TimeSpan remainingTime)
        {
            if (!isRunning || isManuallyPaused)
            {
                return;
            }

            TimeChanged?.Invoke(this, remainingTime);

            if (remainingTime > TimeSpan.Zero)
            {
                ReportSessionState();
                return;
            }

            if (isBreak)
            {
                if (currentPhase == BigDaddySchedulePhase.LongBreak)
                {
                    LongBreakCompleted?.Invoke(this, DateTime.Now);
                }

                StartPlayInterval();
            }
            else
            {
                playedTime += settings.ShortBreakInterval;

                if (playedTime >= settings.LongBreakInterval)
                {
                    playedTime = TimeSpan.Zero;

                    if (settings.LongBreaksEnabled)
                    {
                        StartLongBreak();
                    }
                    else if (settings.ShortBreaksEnabled)
                    {
                        // Keep the normal short-break pattern when only long breaks are disabled.
                        StartShortBreak();
                    }
                    else
                    {
                        StartPlayInterval();
                    }
                }
                else
                {
                    if (settings.ShortBreaksEnabled)
                    {
                        StartShortBreak();
                    }
                    else
                    {
                        StartPlayInterval();
                    }
                }
            }
        }

        // Begins a fresh schedule using the current validated settings.
        public void Start()
        {
            isRunning = true;
            isManuallyPaused = false;
            playedTime = TimeSpan.Zero;
            idleMonitor.SetIdleThreshold(settings.IdleThreshold);
            StartPlayInterval();
        }

        // Restores a persisted schedule. Downtime counts down breaks but never playing time.
        public void Resume(BigDaddySessionState sessionState)
        {
            isRunning = true;
            isManuallyPaused = false;
            playedTime = sessionState.PlayedTime;
            idleMonitor.SetIdleThreshold(settings.IdleThreshold);
            isRestoringSession = true;

            try
            {
                if (sessionState.Phase is BigDaddySchedulePhase.ShortBreak or BigDaddySchedulePhase.LongBreak)
                {
                    TimeSpan remainingBreakTime = sessionState.IsManuallyPaused
                        ? sessionState.RemainingTime
                        : sessionState.BreakEndsAtUtc.HasValue
                            ? sessionState.BreakEndsAtUtc.Value - DateTime.UtcNow
                            : TimeSpan.Zero;

                    if (remainingBreakTime > TimeSpan.Zero)
                    {
                        if (sessionState.Phase == BigDaddySchedulePhase.ShortBreak)
                        {
                            StartShortBreak(remainingBreakTime);
                        }
                        else
                        {
                            StartLongBreak(remainingBreakTime);
                        }

                        RestoreManualPauseIfNeeded(sessionState);
                        return;
                    }
                }

                bool completedLongBreakWhileAway = sessionState.Phase == BigDaddySchedulePhase.LongBreak &&
                    !sessionState.IsManuallyPaused;

                if (completedLongBreakWhileAway)
                {
                    DateTime completedAt = sessionState.BreakEndsAtUtc?.ToLocalTime() ?? DateTime.Now;
                    LongBreakCompleted?.Invoke(this, completedAt);
                }

                TimeSpan remainingPlayTime = sessionState.Phase == BigDaddySchedulePhase.Playing
                    ? sessionState.RemainingTime
                    : settings.ShortBreakInterval;

                StartPlayInterval(remainingPlayTime > TimeSpan.Zero
                    ? remainingPlayTime
                    : settings.ShortBreakInterval);
                RestoreManualPauseIfNeeded(sessionState);
            }
            finally
            {
                isRestoringSession = false;
            }
        }

        public BigDaddySessionState GetSessionState()
        {
            BigDaddySchedulePhase phase = currentPhase ?? BigDaddySchedulePhase.Playing;
            bool hasActiveBreak = phase is BigDaddySchedulePhase.ShortBreak or BigDaddySchedulePhase.LongBreak;

            return new BigDaddySessionState
            {
                Phase = phase,
                RemainingTime = timer.RemainingTime,
                PlayedTime = playedTime,
                BreakEndsAtUtc = hasActiveBreak
                    ? isManuallyPaused ? null : DateTime.UtcNow.Add(timer.RemainingTime)
                    : null,
                IsManuallyPaused = isManuallyPaused
            };
        }

        // Freezes either play or a break for deliberate testing or maintenance.
        public void Pause()
        {
            if (!isRunning || isManuallyPaused)
            {
                return;
            }

            timer.Pause();
            idleMonitor.Stop();
            isManuallyPaused = true;
            ReportSessionState();
        }

        // Continues a manually paused phase without resetting its countdown.
        public void Resume()
        {
            if (!isRunning || !isManuallyPaused)
            {
                return;
            }

            isManuallyPaused = false;

            if (isBreak)
            {
                timer.Resume();
            }
            else
            {
                idleMonitor.Start();
                UpdatePlayTimerForIdleState(idleMonitor.IsIdle);

                if (!playTimerPausedForIdle)
                {
                    timer.Resume();
                }
            }

            ReportSessionState();
        }

        // Ends the current schedule without starting another phase.
        public void Stop()
        {
            timer.Pause();
            idleMonitor.Stop();
            isRunning = false;
            isManuallyPaused = false;
            isBreak = false;
            SetPlayTimerPausedForIdle(false);
        }

        private void RestoreManualPauseIfNeeded(BigDaddySessionState sessionState)
        {
            if (sessionState.IsManuallyPaused)
            {
                Pause();
            }
        }

        // Starts a new play interval and freezes it immediately if the user is already idle.
        private void StartPlayInterval(TimeSpan? duration = null)
        {
            isBreak = false;
            idleMonitor.ResetIdleTimer();
            idleMonitor.Start();
            SetPlayTimerPausedForIdle(false);
            timer.Start(duration ?? settings.ShortBreakInterval);
            SetPhase(BigDaddySchedulePhase.Playing);
            UpdatePlayTimerForIdleState(idleMonitor.IsIdle);
            ReportSessionState();
        }

        // Starts a short break and stops idle monitoring until the next play interval begins.
        private void StartShortBreak(TimeSpan? duration = null)
        {
            isBreak = true;
            idleMonitor.Stop();
            timer.Start(duration ?? settings.ShortBreakDuration);
            SetPhase(BigDaddySchedulePhase.ShortBreak);
            ReportSessionState();
        }

        // Starts a long break and stops idle monitoring until the next play interval begins.
        private void StartLongBreak(TimeSpan? duration = null)
        {
            isBreak = true;
            idleMonitor.Stop();
            timer.Start(duration ?? settings.LongBreakDuration);
            SetPhase(BigDaddySchedulePhase.LongBreak);
            ReportSessionState();
        }

        // Receives active/idle state changes from the Windows idle monitor.
        private void IdleMonitor_IdleStateChanged(object? sender, bool isIdle)
        {
            UpdatePlayTimerForIdleState(isIdle);
        }

        // Freezes and resumes play only; breaks deliberately ignore idle status.
        private void UpdatePlayTimerForIdleState(bool isIdle)
        {
            if (isBreak)
            {
                return;
            }

            if (isIdle && !playTimerPausedForIdle)
            {
                timer.Pause();
                SetPlayTimerPausedForIdle(true);
            }
            else if (!isIdle && playTimerPausedForIdle)
            {
                timer.Resume();
                SetPlayTimerPausedForIdle(false);
            }
        }

        // Stores and reports a phase change only when the phase has actually changed.
        private void SetPhase(BigDaddySchedulePhase phase)
        {
            if (currentPhase == phase)
            {
                return;
            }

            currentPhase = phase;
            PhaseChanged?.Invoke(this, phase);
        }

        // Stores and reports whether play is currently frozen because the user is idle.
        private void SetPlayTimerPausedForIdle(bool isPaused)
        {
            if (playTimerPausedForIdle == isPaused)
            {
                return;
            }

            playTimerPausedForIdle = isPaused;
            PlayIdleStateChanged?.Invoke(this, playTimerPausedForIdle);
            ReportSessionState();
        }

        private void ReportSessionState()
        {
            SessionStateChanged?.Invoke(this, GetSessionState());
        }
    }
}
