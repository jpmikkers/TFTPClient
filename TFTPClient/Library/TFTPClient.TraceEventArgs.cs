using System;

namespace GitHub.JPMikkers.TFTP.Client
{

    public partial class TFTPClient
    {
        public class TraceEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}
