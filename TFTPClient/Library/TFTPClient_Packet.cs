/*

Copyright (c) 2013 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;
using System.IO;
using System.Net;

namespace GitHub.JPMikkers.TFTP.Client
{
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
            }

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
                TFTPPacket result = null;

                long startPosition = s.Position;
                Opcode c = (Opcode)ReadUInt16(s);
                s.Position = startPosition;

                switch (c)
                {
                    case Opcode.Ack:
                        result = new TFTPPacket_Ack(s);
                        break;

                    case Opcode.Data:
                        result = new TFTPPacket_Data(s);
                        break;

                    case Opcode.Error:
                        result = new TFTPPacket_Error(s);
                        break;

                    case Opcode.OptionsAck:
                        result = new TFTPPacket_OptionsAck(s);
                        break;

                    case Opcode.ReadRequest:
                        result = new TFTPPacket_ReadRequest(s);
                        break;

                    case Opcode.WriteRequest:
                        result = new TFTPPacket_WriteRequest(s);
                        break;

                    default:
                        result = new TFTPPacket_Unknown(s);
                        break;
                }

                return result;
            }

            public abstract override string ToString();
        }
    }
}
