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

namespace CodePlex.JPMikkers.TFTP.Client
{
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
                return string.Format("{0}( BlockNumber={1}, Data=[{2}] )", Code, BlockNumber, HexStr(Data, " ", 8));
            }
        }
    }
}
