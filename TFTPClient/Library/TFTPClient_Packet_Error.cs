using System;
using System.IO;

namespace GitHub.JPMikkers.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private class TFTPPacket_Error : TFTPPacket
        {
            public ErrorCode ErrorCode { get; set; }
            public string ErrorMessage { get; set; }

            public TFTPPacket_Error()
                : base()
            {
                Code = Opcode.Error;
                ErrorCode = ErrorCode.Undefined;
                ErrorMessage = "";
            }

            public TFTPPacket_Error(Stream s)
                : this()
            {
                ValidateCode(s);
                ErrorCode = (ErrorCode)ReadUInt16(s);
                ErrorMessage = ReadZString(s);
            }

            public override void Serialize(Stream s)
            {
                base.Serialize(s);
                WriteUInt16(s, (ushort)ErrorCode);
                WriteZString(s, ErrorMessage);
            }

            public override string ToString()
            {
                return $"{Code}( ErrorCode={ErrorCode}, ErrorMessage='{ErrorMessage}' )";
            }
        }
    }
}
