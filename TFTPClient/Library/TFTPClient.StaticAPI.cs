using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient
{
    public static async Task DownloadAsync(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null, CancellationToken cancellationToken = default)
    {
        using var localStream = File.Create(localFilename);
        await DownloadAsync(serverEndPoint, localStream, remoteFilename, settings, cancellationToken);
    }

    public static async Task DownloadAsync(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null, CancellationToken cancellationToken = default)
    {
        using var session = new TFTPClient(serverEndPoint, settings);
        await session.DownloadAsync(remoteFilename, localStream, cancellationToken);
    }

    public static async Task UploadAsync(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null, CancellationToken cancellationToken = default)
    {
        using var localStream = File.OpenRead(localFilename);
        await UploadAsync(serverEndPoint, localStream, remoteFilename, settings, cancellationToken);
    }

    public static async Task UploadAsync(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null, CancellationToken cancellationToken = default)
    {
        using var session = new TFTPClient(serverEndPoint, settings);
        await session.UploadAsync(remoteFilename, localStream, cancellationToken);
    }

    public static void Download(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
    {
        using var localStream = File.Create(localFilename);
        Download(serverEndPoint, localStream, remoteFilename, settings);
    }

    public static void Download(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
    {
        using var session = new TFTPClient(serverEndPoint, settings);
        session.Download(remoteFilename, localStream);
    }

    public static void Upload(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
    {
        using var localStream = File.OpenRead(localFilename);
        Upload(serverEndPoint, localStream, remoteFilename, settings);
    }

    public static void Upload(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
    {
        using var session = new TFTPClient(serverEndPoint, settings);
        session.Upload(remoteFilename, localStream);
    }
}
