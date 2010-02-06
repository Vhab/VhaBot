using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace VhaBot.ShellModules
{
    public class Timers : IDisposable
    {
        public event EventHandler Minute;
        public event EventHandler Hour;
        public event EventHandler EightHours;
        public event EventHandler TwentyFourHours;

        private Timer timer;
        private DateTime lastMinute;
        private DateTime lastHour;
        private DateTime lastEightHours;
        private DateTime lastTwentyFourHours;

        public Timers()
        {
            this.lastMinute = DateTime.Now;
            this.lastHour = DateTime.Now;
            this.lastEightHours = DateTime.Now;
            this.lastTwentyFourHours = DateTime.Now;

            this.timer = new Timer();
            this.timer.AutoReset = true;
            this.timer.Interval = 1000;
            this.timer.Elapsed += new ElapsedEventHandler(Elapsed);
            this.timer.Start();
        }

        private void Elapsed(object sender, ElapsedEventArgs e)
        {
            if (((TimeSpan)(DateTime.Now - this.lastMinute)).TotalSeconds > 60)
            {
                this.lastMinute = DateTime.Now;
                AsyncInvoke.Fire(this.Minute, this, EventArgs.Empty);
            }
            if (((TimeSpan)(DateTime.Now - this.lastHour)).TotalSeconds > 60 * 60)
            {
                this.lastHour = DateTime.Now;
                AsyncInvoke.Fire(this.Hour, this, EventArgs.Empty);
            }
            if (((TimeSpan)(DateTime.Now - this.lastEightHours)).TotalSeconds > 60 * 60 * 8)
            {
                this.lastEightHours = DateTime.Now;
                AsyncInvoke.Fire(this.EightHours, this, EventArgs.Empty);
            }
            if (((TimeSpan)(DateTime.Now - this.lastTwentyFourHours)).TotalSeconds > 60 * 60 * 24)
            {
                this.lastTwentyFourHours = DateTime.Now;
                AsyncInvoke.Fire(this.TwentyFourHours, this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            this.timer.Stop();
            this.timer.Dispose();
        }
    }
}
