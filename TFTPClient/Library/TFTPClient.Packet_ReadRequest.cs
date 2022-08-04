using System;
using System.IO;

namespace Baksteen.Net.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private class TFTPPacket_ReadRequest : TFTPPacket_Request
        {
            public TFTPPacket_ReadRequest()
                : base()
            {
                Code = Opcode.ReadRequest;
            }

            public TFTPPacket_ReadRequest(Stream s)
                : this()
            {
                ValidateCode(s);
                Filename = ReadZString(s);
                Mode = ReadZString(s);
                Options = ReadOptions(s);
            }
        }
    }
}
