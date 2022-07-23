
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