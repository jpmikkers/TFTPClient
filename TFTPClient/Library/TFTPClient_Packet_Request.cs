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
                return string.Format("{0}( FileName='{1}', Mode='{2}', Options={{{3}}} )", Code, Filename, Mode, OptionString(Options));
            }
        }
    }
}
