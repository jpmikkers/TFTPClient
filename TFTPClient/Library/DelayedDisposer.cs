﻿using System;
using System.Threading;

namespace GitHub.JPMikkers.TFTP.Client
{
    public class DelayedDisposer
    {
        private readonly Timer _timer;

        private DelayedDisposer(IDisposable obj, int timeOut)
        {
            _timer = new Timer(x =>
            {
                try
                {

                    obj.Dispose();
                    _timer.Dispose();
                }
                catch
                {
                }
            });
            _timer.Change(timeOut, Timeout.Infinite);
        }

        public static void QueueDelayedDispose(IDisposable obj, int timeOut)
        {
            new DelayedDisposer(obj, timeOut);
        }
    }
}
