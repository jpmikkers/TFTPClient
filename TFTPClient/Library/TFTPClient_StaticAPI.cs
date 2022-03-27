/*

Copyright (c) 2022 Jean-Paul Mikkers

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
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.TFTP.Client
{
    public partial class TFTPClient
    {
        public static async Task DownloadAsync(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (var localStream = File.Create(localFilename))
            {
                await DownloadAsync(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static async Task DownloadAsync(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                await session.DownloadAsync(remoteFilename, localStream);
            }
        }

        public static async Task UploadAsync(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (var localStream = File.OpenRead(localFilename))
            {
                await UploadAsync(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static async Task UploadAsync(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                await session.UploadAsync(remoteFilename, localStream);
            }
        }

        public static void Download(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (var localStream = File.Create(localFilename))
            {
                Download(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Download(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Download(remoteFilename, localStream);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (var localStream = File.OpenRead(localFilename))
            {
                Upload(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Upload(remoteFilename, localStream);
            }
        }
    }
}
