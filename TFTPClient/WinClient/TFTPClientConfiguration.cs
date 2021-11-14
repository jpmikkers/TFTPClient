/*

Copyright (c) 2010 Jean-Paul Mikkers

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

namespace WinClient
{
    [Serializable()]
    public class TFTPClientConfiguration
    {
        public string Server { get; set; }
        public bool IsUpload { get; set; }
        public bool AutoGenerateRemoteFilename { get; set; }
        public string RemoteBaseDirectory { get; set; }
        public string RemoteFilename { get; set; }
        public string LocalFilename { get; set; }

        public int BlockSize { get; set; }
        public int Ttl { get; set; }
        public bool DontFragment { get; set; }
        public int Timeout { get; set; }
        public int Retries { get; set; }

        public TFTPClientConfiguration()
        {
            Server = "localhost:69";
            IsUpload = false;
            AutoGenerateRemoteFilename = true;
            RemoteBaseDirectory = "";
            RemoteFilename = "";
            LocalFilename = "";
            BlockSize = 512;
            Ttl = -1;
            DontFragment = false;
            Timeout = 2;
            Retries = 5;
        }

        public TFTPClientConfiguration Clone()
        {
            var result = new TFTPClientConfiguration();
            result.Server = Server;
            result.IsUpload = IsUpload;
            result.AutoGenerateRemoteFilename = AutoGenerateRemoteFilename;
            result.RemoteBaseDirectory = RemoteBaseDirectory;
            result.RemoteFilename = RemoteFilename;
            result.LocalFilename = LocalFilename;
            result.BlockSize = BlockSize;
            result.Ttl = Ttl;
            result.DontFragment = DontFragment;
            result.Timeout = Timeout;
            result.Retries = Retries;
            return result;
        }
    }
}