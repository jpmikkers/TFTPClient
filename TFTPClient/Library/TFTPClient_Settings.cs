/*

Copyright (c) 2013 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;

namespace CodePlex.JPMikkers.TFTP.Client
{

    public partial class TFTPClient : IDisposable
    {
        public class TraceEventArgs : EventArgs
        {
            public string Message { get; set; }
        }

        public class ProgressEventArgs : EventArgs
        {
            public bool IsUpload { get; set; }
            public string Filename { get; set; }
            public long Transferred { get; set; }
            public long TransferSize { get; set; }
        }

        public class Settings
        {
            public bool DontFragment { get; set; }
            public short Ttl { get; set; }
            public int BlockSize { get; set; }
            public TimeSpan ResponseTimeout { get; set; }
            public int Retries { get; set; }
            public TimeSpan ProgressInterval { get; set; }
            public EventHandler<TraceEventArgs> OnTrace;
            public EventHandler<ProgressEventArgs> OnProgress;

            public Settings()
            {
                DontFragment = false;
                Ttl = -1;
                BlockSize = DefaultBlockSize;
                ResponseTimeout = TimeSpan.FromSeconds(2.0);
                Retries = 5;
                ProgressInterval = TimeSpan.FromSeconds(1.0);
            }
        }
    }
}
