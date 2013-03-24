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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using CodePlex.JPMikkers.TFTP.Client;
using Mono.Options;

namespace Client
{
    class Program
    {
        private static void OnProgress(string filename,long transferred,long transfersize)
        {
            Console.WriteLine("'{0}': {1} of {2}",filename,transferred,transfersize >= 0 ? transfersize.ToString() : "?");
        }

        private static void OnTrace(string msg)
        {
            Console.WriteLine(msg);
        }

        static int Main(string[] args)
        {
            string localFilename = null;
            string remoteFilename = null;
            bool isGet = false;
            bool isPut = false;
            bool ipv6 = false;
            int serverPort = 69;
            bool showHelp = false;
            IPAddress serverAddress = null;

            var settings = new TFTPClient.Settings()
            {
                ProgressInterval = TimeSpan.FromMilliseconds(200.0),
                OnProgress = Program.OnProgress
            };

            var optionSet = new OptionSet 
            {
                "Usage: TFTPClient [options]+ host[:port]",
                "",
                { "get", "get a file from remote to local", x => { isGet=true; } },
                { "put", "put a file from local to remote", x => { isPut=true; } },
                { "local=", "local filename", name => localFilename = name },
                { "remote=", "remote filename", name => remoteFilename = name },
                { "p|serverport=", "override server port (default: 69)", (int port) => serverPort = port },
                { "b|blocksize=", "set blocksize (default: 512)", (int blocksize) => settings.BlockSize = blocksize },
                { "t|timeout=", "set response timeout [s] (default: 2)", (int timeout) => settings.ResponseTimeout = TimeSpan.FromSeconds(timeout) },
                { "r|retries=", "set maximum retries (default: 5)", (int retries) => settings.Retries = retries },
                { "v|verbose", "generate verbose tracing", x => { settings.OnTrace = Program.OnTrace; } },
                { "6|ipv6", "resolve hostname to an ipv6 address", x => { ipv6=true; } },
                { "dontfragment", "don't allow packet fragmentation (default: allowed)", x => settings.DontFragment = (x!=null) },
                { "ttl=", "set time to live", (short ttl) => settings.Ttl = ttl },
                { "?|h|help", "show help", x => { showHelp=true; } },
            };

            var remaining=optionSet.Parse(args);

            if (showHelp)
            {
                Console.WriteLine("Help:");
                optionSet.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (!isGet && !isPut)
            {
                Console.Error.WriteLine("You have to specify /get or /put");
                return 1;
            }

            if (localFilename == null || remoteFilename == null)
            {
                Console.Error.WriteLine("/local and /remote are mandatory");
                return 1;
            }

            if (remaining.Count < 1)
            {
                Console.Error.WriteLine("You have to specify the server host name or ip address");
                return 1;
            }

            if (IPAddress.TryParse(remaining[0], out serverAddress))
            {
                // now try to find whether a port was specified
                var components = remaining[0].Split(new string[] { serverAddress.AddressFamily == AddressFamily.InterNetworkV6 ? "]:" : ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (components.Length > 1) int.TryParse(components[1], out serverPort);
            }
            else
            {
                var components = remaining[0].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                serverAddress = Dns.GetHostEntry(components[0]).AddressList.Where(x => x.AddressFamily == (ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork)).FirstOrDefault();
                if (components.Length > 1) int.TryParse(components[1], out serverPort);
            }

            if (serverAddress == null)
            {
                Console.Error.WriteLine("Could not resolve '{0}' to an {1} address", remaining[0], ipv6 ? "ipv6" : "ipv4");
                return 1;
            }

            try
            {
                if (isPut)
                {
                    TFTPClient.Upload(
                        new IPEndPoint(serverAddress, serverPort),
                        localFilename,
                        remoteFilename,
                        settings);
                }
                else
                {
                    TFTPClient.Download(
                        new IPEndPoint(serverAddress, serverPort),
                        localFilename,
                        remoteFilename,
                        settings);
                }
                return 0;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("Transfer failed: {0}",e.Message);
                return 1;
            }
        }
    }
}
