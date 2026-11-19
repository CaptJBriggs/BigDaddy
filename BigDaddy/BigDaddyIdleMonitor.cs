using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BigDaddy
{
    // Watches Windows keyboard and mouse activity and reports idle/active changes.
    internal class BigDaddyIdleMonitor
    {
        // Amount of inactivity required before Big Daddy considers the user idle.
        private TimeSpan idleThreshold;
        // Checks Windows once a second without blocking the user interface.
        private readonly System.Windows.Forms.Timer timer;
        private bool isIdle;

        // Tick count recorded when the current play interval starts.
        private long idleTrackingStartedAt;

        public event EventHandler<bool>? IdleStateChanged;

        // Asks Windows whether the user has been inactive for at least the chosen threshold.
        public bool IsIdle => GetIdleTime() >= idleThreshold;

        public BigDaddyIdleMonitor(TimeSpan idleThreshold)
        {
            this.idleThreshold = idleThreshold;
            idleTrackingStartedAt = Environment.TickCount64;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
        }

        // Starts monitoring and remembers the current state as the starting point.
        public void Start()
        {
            isIdle = IsIdle;
            timer.Start();
        }

        // Stops checking Windows while the scheduler is in a break phase.
        public void Stop()
        {
            timer.Stop();
        }

        // Starts a fresh inactivity period when a new play interval begins.
        public void ResetIdleTimer()
        {
            isIdle = false;
            idleTrackingStartedAt = Environment.TickCount64;
        }

        // Replaces the idle threshold before a new schedule starts.
        public void SetIdleThreshold(TimeSpan idleThreshold)
        {
            this.idleThreshold = idleThreshold;
        }

        // Runs every second and raises an event only when activity status actually changes.
        private void Timer_Tick(object? sender, EventArgs e)
        {
            bool currentIdleState = IsIdle;

            if (currentIdleState == isIdle)
            {
                return;
            }

            isIdle = currentIdleState;
            IdleStateChanged?.Invoke(this, isIdle);
        }

        // Uses Windows' last keyboard or mouse input timestamp to calculate idle time.
        private TimeSpan GetIdleTime()
        {
            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return TimeSpan.Zero;
            }

            uint currentTickCount = unchecked((uint)Environment.TickCount);
            uint idleMilliseconds = currentTickCount - lastInputInfo.dwTime;

            // Only count inactivity that happened after this play interval began.
            long trackedMilliseconds = Math.Max(0, Environment.TickCount64 - idleTrackingStartedAt);
            double effectiveIdleMilliseconds = Math.Min(idleMilliseconds, trackedMilliseconds);

            return TimeSpan.FromMilliseconds(effectiveIdleMilliseconds);
        }

        // Windows API method that provides the last keyboard or mouse input time.
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

        // Exact data layout expected by the Windows API method above.
        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }
    }
}
