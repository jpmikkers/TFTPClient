# Basic TFTP client library usage

## Example 1 : Upload a file

The following example shows how to upload a local file "localfile.bin" to a tftp server located at 192.168.1.10:69, and name the remote file "remotefile.bin".

    using System.Net;
    using System.Net.Sockets;
    using Baksteen.Net.TFTP.Client;
    ...
    TFTPClient.Upload(
        new IPEndPoint(IPAddress.Parse("192.168.1.10"), 69),
        "localfile.bin",
        "remotefile.bin");

## Example 2 : Download a file

The following example shows how to download a remote file "remotefile.bin" from a tftp server located at 192.168.1.10:69, and name the local file "localfile.bin".

    using System.Net;
    using System.Net.Sockets;
    using Baksteen.Net.TFTP.Client;
    ...
    TFTPClient.Download(
        new IPEndPoint(IPAddress.Parse("192.168.1.10"), 69),
        "localfile.bin",
        "remotefile.bin");

