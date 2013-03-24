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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CodePlex.JPMikkers.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        #region TFTP definitions

        internal enum ErrorCode : ushort
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

        internal enum Opcode : ushort
        {
            Unknown = 0,
            ReadRequest = 1,
            WriteRequest,
            Data,
            Ack,
            Error,
            OptionsAck
        }

        internal const string Option_Multicast = "multicast";
        internal const string Option_Timeout = "timeout";
        internal const string Option_TransferSize = "tsize";
        internal const string Option_BlockSize = "blksize";

        #endregion TFTP definitions

        #region Packets
        internal abstract class TFTPPacket
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

            #region packet serialization/deserialization
            internal static ushort ReadUInt16(Stream s)
            {
                var br = new BinaryReader(s);
                return (ushort)IPAddress.NetworkToHostOrder((short)br.ReadUInt16());
            }

            internal static void WriteUInt16(Stream s, ushort v)
            {
                var bw = new BinaryWriter(s);
                bw.Write((ushort)IPAddress.HostToNetworkOrder((short)v));
            }

            internal static Dictionary<string, string> ReadOptions(Stream s)
            {
                var options = new Dictionary<string, string>();
                while (s.Position < s.Length)
                {
                    string key = ReadZString(s).ToLower();
                    string val = ReadZString(s).ToLower();
                    options.Add(key, val);
                }
                return options;
            }

            internal static void WriteOptions(Stream s, Dictionary<string, string> options)
            {
                foreach (var option in options)
                {
                    WriteZString(s, option.Key);
                    WriteZString(s, option.Value);
                }
            }

            internal static string ReadZString(Stream s)
            {
                var sb = new StringBuilder();
                int c = s.ReadByte();
                while (c > 0)
                {
                    sb.Append((char)c);
                    c = s.ReadByte();
                }
                return sb.ToString();
            }

            internal static void WriteZString(Stream s, string msg)
            {
                var tw = new StreamWriter(s, Encoding.ASCII);
                tw.Write(msg);
                tw.Flush();
                s.WriteByte(0);
            }
            #endregion
        }
        #endregion

        public static string OptionString(Dictionary<string, string> options)
        {
            return options.Select(x => String.Format("'{0}'='{1}'", x.Key, x.Value)).Aggregate((x, y) => x + ", " + y);
        }

        public static string HexStr(ArraySegment<byte> data, string separator, int limit)
        {
            var sb = new StringBuilder();
            limit = Math.Min(data.Count, limit);

            for (int t = 0; t < limit; t++)
            {
                sb.Append(data.Array[data.Offset + t].ToString("X2"));
                sb.Append(separator);
            }

            if (data.Count > limit)
            {
                sb.Append("..");
            }
            else
            {
                if (sb.Length > separator.Length) sb.Length = sb.Length - separator.Length;
            }

            return sb.ToString();
        }

    }
}
