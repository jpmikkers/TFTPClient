using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.JPMikkers.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private abstract class TFTPPacket_Request : TFTPPacket
        {
            public string Filename { get; set; }
            public string Mode { get; set; }
            public Dictionary<string, string> Options { get; set; }

            protected TFTPPacket_Request()
            {
                Code = Opcode.Unknown;
                Filename = "";
                Mode = "octet";
                Options = new Dictionary<string, string>();
            }

            public override void Serialize(Stream s)
            {
                base.Serialize(s);
                WriteZString(s, Filename);
                WriteZString(s, Mode);
                WriteOptions(s, Options);
            }

            public override string ToString()
            {
                return $"{Code}( FileName='{Filename}', Mode='{Mode}', Options={{{OptionString(Options)}}} )";
            }
        }
    }
}
