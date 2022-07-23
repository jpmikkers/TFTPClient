
using GitHub.JPMikkers.TFTP.Client;
using Mono.Options;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Client
{
    class Program
    {
        private static void OnProgress(object sender, TFTPClient.ProgressEventArgs args)
        {
            Console.WriteLine($"'{args.Filename}': {args.Transferred} of {(args.TransferSize >= 0 ? args.TransferSize.ToString() : "?")}");
        }

        private static void OnTrace(object sender, TFTPClient.TraceEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        static int Main(string[] args)
        {
            try
            {
                string localFilename = null;
                string remoteFilename = null;
                bool isGet = false;
                bool isPut = false;
                bool ipv6 = false;
                int serverPort = 69;
                bool showHelp = false;
                bool silent = false;
                IPAddress serverAddress = null;

                var settings = new TFTPClient.Settings()
                {
                    ProgressInterval = TimeSpan.FromMilliseconds(200.0),
                    OnProgress = Program.OnProgress
                };

                var optionSet = new OptionSet
                {
                    "",
                    $"TFTPClient {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}",
                    "Transfers files to and from a remote computer running the TFTP service.",
                    "",
                    "Usage: TFTPClient [options]+ host[:port]",
                    "",
                    { "get", "get a file from remote to local", x => { isGet=true; } },
                    { "put", "put a file from local to remote", x => { isPut=true; } },
                    { "local=", "local filename", name => localFilename = name },
                    { "remote=", "remote filename", name => remoteFilename = name },
                    { "serverport=", "override server port (default: 69)", (int port) => serverPort = port },
                    { "blocksize=", "set blocksize (default: 512)", (int blocksize) => settings.BlockSize = blocksize },
                    { "timeout=", "set response timeout [s] (default: 2)", (int timeout) => settings.ResponseTimeout = TimeSpan.FromSeconds(timeout) },
                    { "retries=", "set maximum retries (default: 5)", (int retries) => settings.Retries = retries },
                    { "verbose", "generate verbose tracing", x => { settings.OnTrace = Program.OnTrace; } },
                    { "ipv6", "resolve hostname to an ipv6 address", x => { ipv6=true; } },
                    { "dontfragment", "don't allow packet fragmentation (default: allowed)", x => settings.DontFragment = (x!=null) },
                    { "silent", "don't show progress information", x => silent = (x!=null) },
                    { "ttl=", "set time to live", (short ttl) => settings.Ttl = ttl },
                    { "?|h|help", "show help", x => { showHelp=true; } },
                    "",
                    "You may use -, --, or / as option delimiters",
                    "",
                    "'host' may be specified as a hostname, ipv4 address or ipv6 address.",
                    "To specify the port number for a ipv6 address you should put the address",
                    "in square brackets, e.g. [::1]:69",
                    "",
                    "Example: (downloading a file 'image.bin' from a server at 192.168.1.23)",
                    "\tTFTPClient /get /local=image.bin /remote=image.bin 192.168.1.23",
                };

                var remaining = optionSet.Parse(args);

                if (showHelp || remaining.Count < 1)
                {
                    optionSet.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                if (silent)
                {
                    settings.OnProgress = null;
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
                    Console.Error.WriteLine($"Could not resolve '{remaining[0]}' to an {(ipv6 ? "ipv6" : "ipv4")} address");
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
                    if (!silent) Console.WriteLine("Transfer complete.");
                    return 0;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Transfer failed: {e.Message}");
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                return 1;
            }
        }
    }
}
