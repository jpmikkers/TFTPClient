using System;
using System.IO;
using System.Net;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
    #region TFTP definitions

    private enum ErrorCode : ushort
    {
        Undefined = 0,
        FileNotFound,
        AccessViolation,
        DiskFull,
        IllegalOperation,
        UnknownTransferID,
        FileAlreadyExists,
        NoSuchUser
    }

    private enum Opcode : ushort
    {
        Unknown = 0,
        ReadRequest = 1,
        WriteRequest,
        Data,
        Ack,
        Error,
        OptionsAck
    }

    private const string Option_Multicast = "multicast";
    private const string Option_Timeout = "timeout";
    private const string Option_TransferSize = "tsize";
    private const string Option_BlockSize = "blksize";

    #endregion TFTP definitions

    private abstract class TFTPPacket
    {
        public Opcode Code { get; protected set; }

        public IPEndPoint EndPoint
        {
            get;
            set;
        } = new IPEndPoint(IPAddress.None, 0);

        public virtual void Serialize(Stream s)
        {
            WriteUInt16(s, (ushort)Code);
        }

        protected void ValidateCode(Stream s)
        {
            if ((Opcode)ReadUInt16(s) != Code) throw new InvalidDataException();
        }

        public static TFTPPacket Deserialize(Stream s)
        {
            TFTPPacket result;

            long startPosition = s.Position;
            Opcode c = (Opcode)ReadUInt16(s);
            s.Position = startPosition;

            switch (c)
            {
                case Opcode.Ack:
                    result = TFTPPacket_Ack.Deserialize(s);
                    break;

                case Opcode.Data:
                    result = TFTPPacket_Data.Deserialize(s);
                    break;

                case Opcode.Error:
                    result = TFTPPacket_Error.Deserialize(s);
                    break;

                case Opcode.OptionsAck:
                    result = TFTPPacket_OptionsAck.Deserialize(s);
                    break;

                case Opcode.ReadRequest:
                    result = TFTPPacket_ReadRequest.Deserialize(s);
                    break;

                case Opcode.WriteRequest:
                    result = TFTPPacket_WriteRequest.Deserialize(s);
                    break;

                default:
                    result = TFTPPacket_Unknown.Deserialize(s);
                    break;
            }

            return result;
        }

        public abstract override string ToString();
    }
}
