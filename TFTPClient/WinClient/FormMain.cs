using GitHub.JPMikkers.TFTP.Client;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinClient
{
    public partial class FormMain : Form
    {
        private Task task;

        private TFTPClientConfiguration Configuration
        {
            get
            {
                return Properties.Settings.Default.Setting ?? new TFTPClientConfiguration();
            }
            set
            {
                try
                {
                    Properties.Settings.Default.Setting = value;
                    Properties.Settings.Default.Save();
                }
                catch
                {
                }
            }
        }


        private bool IsDownload
        {
            get
            {
                return comboBoxOperation.SelectedIndex == 0;
            }
        }

        private bool AutomaticRemoteFilename
        {
            get
            {
                return checkBoxAutoGenerateRemoteFilename.Checked;
            }
        }

        private string LocalFilename
        {
            get
            {
                return textBoxLocalFilename.Text.Trim();
            }
        }

        private string RemoteFilename
        {
            get
            {
                return textBoxRemoteFilename.Text.Trim();
            }
        }

        public FormMain()
        {
            InitializeComponent();
            comboBoxOperation.SelectedIndex = 0;
            UpdateAutoNameState();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            var settings = Configuration;
            this.textBoxServer.Text = settings.Server;
            this.comboBoxOperation.SelectedIndex = settings.IsUpload ? 1 : 0;
            this.checkBoxAutoGenerateRemoteFilename.Checked = settings.AutoGenerateRemoteFilename;
            this.textBoxRemoteBaseDirectory.Text = settings.RemoteBaseDirectory;
            this.textBoxRemoteFilename.Text = settings.RemoteFilename;
            this.textBoxLocalFilename.Text = settings.LocalFilename;
            Configuration = settings;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = Configuration;
            settings.Server = this.textBoxServer.Text;
            settings.IsUpload = this.comboBoxOperation.SelectedIndex == 1;
            settings.AutoGenerateRemoteFilename = this.checkBoxAutoGenerateRemoteFilename.Checked;
            settings.RemoteBaseDirectory = this.textBoxRemoteBaseDirectory.Text;
            settings.RemoteFilename = this.textBoxRemoteFilename.Text;
            settings.LocalFilename = this.textBoxLocalFilename.Text;
            Configuration = settings;
        }
        /*
                void textBoxServer_Validated(object sender, EventArgs e)
                {
                    this.errorProvider1.SetError(textBoxServer, "");
                }

                private void textBoxServer_Validating(object sender, CancelEventArgs e)
                {
                    ushort value;

                    e.Cancel = true;

                    if (ResolveServer(textBoxServer.Text) != null)
                    {
                        e.Cancel = false;
                    }

                    if (e.Cancel)
                    {
                        this.errorProvider1.SetError(textBoxServer, "Not a valid server address");
                    }
                }
        */

        private void button3_Click(object sender, EventArgs e)
        {
            if (IsDownload)
            {
                if (saveFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    textBoxLocalFilename.Text = saveFileDialog1.FileName;
                    UpdateAutoName();
                }
            }
            else
            {
                if (openFileDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    textBoxLocalFilename.Text = openFileDialog1.FileName;
                    UpdateAutoName();
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAutoNameState();
        }

        private void UpdateAutoNameState()
        {
            if (checkBoxAutoGenerateRemoteFilename.Checked)
            {
                textBoxRemoteFilename.ReadOnly = true;
                textBoxRemoteBaseDirectory.Enabled = true;
                UpdateAutoName();
            }
            else
            {
                textBoxRemoteFilename.ReadOnly = false;
                textBoxRemoteBaseDirectory.Text = "";
                textBoxRemoteBaseDirectory.Enabled = false;
                UpdateAutoName();
            }
        }

        private void UpdateAutoName()
        {
            if (AutomaticRemoteFilename)
            {
                textBoxRemoteFilename.Text = Path.Combine(textBoxRemoteBaseDirectory.Text, Path.GetFileName(textBoxLocalFilename.Text));
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateAutoName();
        }

        private void textBoxLocalFilename_TextChanged(object sender, EventArgs e)
        {
            UpdateAutoName();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (task == null)
                {
                    toolStripStatusLabel1.Text = "";

                    if (string.IsNullOrWhiteSpace(LocalFilename))
                    {
                        MessageBox.Show(this, "Please enter a valid local filename", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(RemoteFilename))
                    {
                        MessageBox.Show(this, "Please enter a valid remote filename", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    IPEndPoint endpoint = ResolveServer(textBoxServer.Text);
                    string localFilename = LocalFilename;
                    string remoteFilename = RemoteFilename;
                    bool isDownload = IsDownload;
                    var settings = new TFTPClient.Settings();
                    settings.OnProgress = OnProgress;
                    settings.ProgressInterval = TimeSpan.FromMilliseconds(200);
                    settings.BlockSize = Configuration.BlockSize;
                    settings.DontFragment = Configuration.DontFragment;
                    settings.ResponseTimeout = TimeSpan.FromSeconds(Configuration.Timeout);
                    settings.Retries = Configuration.Retries;
                    settings.Ttl = (short)Configuration.Ttl;

                    panel1.Enabled = false;

                    task = Task.Factory.StartNew(
                        () =>
                        {
                            if (isDownload)
                            {
                                TFTPClient.Download(endpoint, localFilename, remoteFilename, settings);
                            }
                            else
                            {
                                TFTPClient.Upload(endpoint, localFilename, remoteFilename, settings);
                            }
                        },
                        TaskCreationOptions.LongRunning
                    ).ContinueWith(
                        t =>
                        {
                            if (t.Exception != null)
                            {
                                HandleException(t.Exception.InnerException);
                            }
                            panel1.Enabled = true;
                            task = null;
                        }
                    , uiScheduler);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgress(object sender, TFTPClient.ProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<TFTPClient.ProgressEventArgs>(OnProgress), sender, e);
            }
            else
            {
                toolStripStatusLabel1.Text = $"({e.Transferred}/{((e.TransferSize >= 0) ? e.TransferSize.ToString() : "?")} bytes) {(e.IsUpload ? "Uploading" : "Downloading")} '{e.Filename}'";
                toolStripProgressBar1.Value = (e.TransferSize > 0) ? (int)(100.0 * e.Transferred / e.TransferSize) : 0;
            }
        }

        private void HandleException(Exception e)
        {
            toolStripStatusLabel1.Text = $"Error: '{e.Message}'";
            MessageBox.Show(this, e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Parses/resolves a string to an endpoint, supporting the following formats:
        /// x.x.x.x                 -> 127.0.0.1
        /// x.x.x.x:p               -> 127.0.0.1:69
        /// xxxx:xxxx:xxxx:xxxx     -> fe80::6982:bedb:3ffd:5741
        /// [xxxx:xxxx:xxxx:xxxx]:p -> [fe80::6982:bedb:3ffd:5741]:69
        /// hostname                -> localhost
        /// hostname:p              -> localhost:69
        /// </summary>
        /// <param name="server">string to parse</param>
        /// <returns>the IPEndPoint or null</returns>
        private static IPEndPoint ResolveServer(string server)
        {
            // nested functions are cool
            Func<string, int, int> ParseIntDefault = (str, def) =>
            {
                int val;
                return int.TryParse(str, out val) ? val : def;
            };

            IPEndPoint result = null;
            IPAddress address;
            int port = 69;

            // attempt to parse it as a ipv6 address
            var parts = server.Split(new string[] { "[", "]:" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && IPAddress.TryParse(parts[0], out address))
            {
                if (parts.Length > 1) port = ParseIntDefault(parts[1], 69);
                result = new IPEndPoint(address, port);
            }
            else
            {
                // no luck, try it as a ipv4 address
                parts = server.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    if (parts.Length > 1) port = ParseIntDefault(parts[1], 69);

                    if (IPAddress.TryParse(parts[0], out address))
                    {
                        result = new IPEndPoint(address, port);
                    }
                    else
                    {
                        // still nothing, resolve the hostname
                        var addressList = Dns.GetHostEntry(parts[0]).AddressList;

                        // prefer ipv4 addresses, fall back to ipv6
                        address = addressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault() ??
                                    addressList.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).FirstOrDefault();

                        if (address != null)
                        {
                            result = new IPEndPoint(address, port);
                        }
                    }
                }
            }

            if (result == null)
            {
                throw new ArgumentException("Couldn't resolve the hostname or IP address");
            }

            return result;
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new FormSettings())
            {
                settingsForm.StartPosition = FormStartPosition.CenterParent;
                settingsForm.Configuration = Configuration;
                if (settingsForm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    Configuration = settingsForm.Configuration;
                }
            }
        }
    }
}
