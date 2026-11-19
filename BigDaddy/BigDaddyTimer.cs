using System;
using System.Windows.Forms;

namespace BigDaddy
{
    // Counts down one current interval and reports the remaining time to the scheduler.
    public class BigDaddyTimer
    {
        // Windows Forms timer that raises a Tick event on the user-interface thread.
        private System.Windows.Forms.Timer timer;
        // Remaining time in the current play or break interval.
        private TimeSpan remainingTime;

        public event EventHandler<TimeSpan>? TimeChanged;

        public TimeSpan RemainingTime => remainingTime;

        public BigDaddyTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
        }

        // Removes one second whenever the timer ticks and tells the scheduler about the result.
        private void Timer_Tick(object? sender, EventArgs e)
        {
            remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(1));

            if (remainingTime <= TimeSpan.Zero)
            {
                remainingTime = TimeSpan.Zero;
                timer.Stop();
            }

            TimeChanged?.Invoke(this, remainingTime);
        }

        // Starts a new interval and immediately reports its full starting time.
        public void Start(TimeSpan duration)
        {
            remainingTime = duration;
            TimeChanged?.Invoke(this, remainingTime);
            timer.Start();
        }

        // Freezes the current interval without changing its remaining time.
        public void Pause()
        {
            timer.Stop();
        }

        // Continues a previously paused interval from its existing remaining time.
        public void Resume()
        {
            timer.Start();
        }
    }
}
