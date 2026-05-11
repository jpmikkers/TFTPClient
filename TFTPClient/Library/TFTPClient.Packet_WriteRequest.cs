using System;
using System.IO;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
    private class TFTPPacket_WriteRequest : TFTPPacket_Request
    {
        public TFTPPacket_WriteRequest()
            : base()
        {
            Code = Opcode.WriteRequest;
        }

        private TFTPPacket_WriteRequest(Stream s)
            : this()
        {
            ValidateCode(s);
            Filename = ReadZString(s);
            Mode = ReadZString(s);
            Options = ReadOptions(s);
        }

        public static new TFTPPacket_WriteRequest Deserialize(Stream s)
        {
            return new TFTPPacket_WriteRequest(s);
        }
    }
}
