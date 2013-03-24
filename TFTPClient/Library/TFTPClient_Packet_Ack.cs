﻿/*

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
        private class TFTPPacket_Ack : TFTPPacket
        {
            public ushort BlockNumber { get; set; }

            public TFTPPacket_Ack()
                : base()
            {
                Code = Opcode.Ack;
                BlockNumber = 0;
            }

            public TFTPPacket_Ack(Stream s)
                : this()
            {
                ValidateCode(s);
                BlockNumber = ReadUInt16(s);
            }

            public override void Serialize(Stream s)
            {
                base.Serialize(s);
                WriteUInt16(s, BlockNumber);
            }

            public override string ToString()
            {
                return string.Format("{0}( BlockNumber={1} )", Code, BlockNumber);
            }
        }
    }
}
