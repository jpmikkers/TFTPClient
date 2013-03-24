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
                return string.Format("{0}( ErrorCode={1}, ErrorMessage='{2}' )", Code, ErrorCode, ErrorMessage);
            }
        }
    }
}
