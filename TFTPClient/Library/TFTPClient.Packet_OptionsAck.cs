using System;
using System.Collections.Generic;
using System.IO;

namespace Baksteen.Net.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private class TFTPPacket_OptionsAck : TFTPPacket
        {
            public Dictionary<string, string> Options { get; set; }

            public TFTPPacket_OptionsAck()
                : base()
            {
                Code = Opcode.OptionsAck;
                Options = new Dictionary<string, string>();
            }

            public TFTPPacket_OptionsAck(Stream s)
                : this()
            {
                ValidateCode(s);
                Options = ReadOptions(s);
            }

            public override void Serialize(Stream s)
            {
                base.Serialize(s);
                WriteOptions(s, Options);
            }

            public override string ToString()
            {
                return $"{Code}( Options={{{OptionString(Options)}}} )";
            }
        }
    }
}
