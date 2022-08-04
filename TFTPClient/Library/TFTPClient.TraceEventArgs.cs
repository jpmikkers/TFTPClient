using System;

namespace Baksteen.Net.TFTP.Client
{

    public partial class TFTPClient
    {
        public class TraceEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}
