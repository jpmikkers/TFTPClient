using System;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
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
