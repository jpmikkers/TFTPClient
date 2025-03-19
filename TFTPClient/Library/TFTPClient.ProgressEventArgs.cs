using System;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient
{
    public class ProgressEventArgs : EventArgs
    {
        public bool IsUpload { get; set; }
        public string Filename { get; set; }
        public long Transferred { get; set; }
        public long TransferSize { get; set; }
    }
}
