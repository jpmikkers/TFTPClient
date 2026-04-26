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

        private TFTPPacket_Unknown(Stream s)
            : this()
        {
            var data = new byte[s.Length];
            s.ReadExactly(data, 0, data.Length);
            Data = new ArraySegment<byte>(data);
        }

        public static new TFTPPacket_Unknown Deserialize(Stream s)
        {
            return new TFTPPacket_Unknown(s);
        }

        public override void Serialize(Stream s)
        {
            if (Data.Array != null)
            {
                s.Write(Data.Array, Data.Offset, Data.Count);
            }
        }

        public override string ToString()
        {
            return $"{Code}( Data=[{HexStr(Data, " ", 8)}] )";
        }
    }
}
