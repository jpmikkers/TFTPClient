using System;
using System.IO;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
    private class TFTPPacket_Unknown : TFTPPacket
    {
        public ArraySegment<byte> Data { get; set; }

        public TFTPPacket_Unknown()
            : base()
        {
            Code = Opcode.Unknown;
            Data = new ArraySegment<byte>();
        }

        public TFTPPacket_Unknown(Stream s)
            : this()
        {
            var data = new byte[s.Length];
            s.Read(data, 0, data.Length);
            Data = new ArraySegment<byte>(data);
        }

        public override void Serialize(Stream s)
        {
            s.Write(Data.Array, Data.Offset, Data.Count);
        }

        public override string ToString()
        {
            return $"{Code}( Data=[{HexStr(Data, " ", 8)}] )";
        }
    }
}
