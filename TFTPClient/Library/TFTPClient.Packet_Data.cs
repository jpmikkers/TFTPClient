using System;
using System.IO;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
    private class TFTPPacket_Data : TFTPPacket
    {
        public ushort BlockNumber { get; set; }
        public ArraySegment<byte> Data { get; set; }

        public TFTPPacket_Data()
            : base()
        {
            Code = Opcode.Data;
            Data = new ArraySegment<byte>();
        }

        public TFTPPacket_Data(Stream s)
            : this()
        {
            ValidateCode(s);
            BlockNumber = ReadUInt16(s);
            byte[] data = new byte[s.Length - s.Position];
            int bytesRead = s.Read(data, 0, data.Length);
            Data = new ArraySegment<byte>(data);
        }

        public override void Serialize(Stream s)
        {
            base.Serialize(s);
            WriteUInt16(s, BlockNumber);
            s.Write(Data.Array, Data.Offset, Data.Count);
        }

        public override string ToString()
        {
            return $"{Code}( BlockNumber={BlockNumber}, Data=[{HexStr(Data, " ", 8)}] )";
        }
    }
}
