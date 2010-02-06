using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VhaBot
{
    public class AsyncInvoke
    {
        public static void Fire(Delegate del, params object[] args)
        {
            if (del == null)
                return;

            Delegate[] delegates = del.GetInvocationList();
            foreach (Delegate sink in delegates)
            {
                AsyncInvoke invoke = new AsyncInvoke(sink, args);
                invoke.BeginInvoke();
            }
        }

        private Delegate _sink;
        private object[] _args;
        private Thread _thread;
        private object _result;
        public AsyncInvoke(Delegate sink, object[] args)
        {
            this._sink = sink;
            this._args = args;
        }

        public void BeginInvoke()
        {
            if (this._thread != null)
                return;
            this._thread = new Thread(new ThreadStart(this.Run));

            this._thread.Start();
        }

        private void Run()
        {
            try
            {
                this._result = this._sink.DynamicInvoke(this._args);
            }
            catch { }
        }

        public object EndInvoke()
        {
            if (this._thread == null)
                return null;
            if (this._thread.ThreadState == ThreadState.Running)
                this._thread.Join();
            return this._result;
        }
    }
}
