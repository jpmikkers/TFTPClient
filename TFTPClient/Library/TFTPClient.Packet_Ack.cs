using System;
using System.IO;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
    private class TFTPPacket_Ack : TFTPPacket
    {
        public ushort BlockNumber { get; set; }

        public TFTPPacket_Ack()
            : base()
        {
            Code = Opcode.Ack;
            BlockNumber = 0;
        }

        private TFTPPacket_Ack(Stream s)
            : this()
        {
            ValidateCode(s);
            BlockNumber = ReadUInt16(s);
        }

        public static new TFTPPacket_Ack Deserialize(Stream s)
        {
            return new TFTPPacket_Ack(s);
        }

        public override void Serialize(Stream s)
        {
            base.Serialize(s);
            WriteUInt16(s, BlockNumber);
        }

        public override string ToString()
        {
            return $"{Code}( BlockNumber={BlockNumber} )";
        }
    }
}
