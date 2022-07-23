using System;
using System.IO;

namespace GitHub.JPMikkers.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private class TFTPPacket_WriteRequest : TFTPPacket_Request
        {
            public TFTPPacket_WriteRequest()
                : base()
            {
                Code = Opcode.WriteRequest;
            }

            public TFTPPacket_WriteRequest(Stream s)
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
