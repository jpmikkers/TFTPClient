
using System;
using System.Windows.Forms;

namespace WinClient
{
    public partial class FormSettings : Form
    {
        private TFTPClientConfiguration _configuration;

        public TFTPClientConfiguration Configuration
        {
            get
            {
                return _configuration.Clone();
            }
            set
            {
                _configuration = value.Clone();
                Bind();
            }
        }

        public FormSettings()
        {
            InitializeComponent();
            /*
                        textBoxWindowSize.Validating += new CancelEventHandler(textBoxWindowSize_Validating);
                        textBoxWindowSize.Validated += new EventHandler(textBoxWindowSize_Validated);
                        toolTip1.SetToolTip(textBoxWindowSize, 
                            "The number of packets to send in bulk, speeding up the file transfer rate.\r\n" +
                            "This is an advanced option, only use a value greater than 1 if you've\r\n" +
                            "tested that your TFTP client can cope with windowed transfers.\r\n" +
                            "(default: 1)");
             */
        }

        /*
                void textBoxWindowSize_Validated(object sender, EventArgs e)
                {
                    this.errorProvider1.SetError(textBoxWindowSize, "");
                }

                private void textBoxWindowSize_Validating(object sender, CancelEventArgs e)
                {
                    ushort value;

                    e.Cancel = true;
                    if (ushort.TryParse(textBoxWindowSize.Text, out value))
                    {
                        if (value > 0 && value <= 32) e.Cancel = false;
                    }

                    if (e.Cancel)
                    {
                        this.errorProvider1.SetError(textBoxWindowSize, "value must be between 1 and 32 (default: 1)");
                    }
                }
        */
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        private void Bind()
        {
            textBoxBlockSize.DataBindings.Clear();
            textBoxTTL.DataBindings.Clear();
            textBoxTimeout.DataBindings.Clear();
            textBoxRetries.DataBindings.Clear();
            checkBoxDontFragment.DataBindings.Clear();
            BindingSource bs = new BindingSource(_configuration, null);
            textBoxBlockSize.DataBindings.Add("Text", bs, "BlockSize");
            checkBoxDontFragment.DataBindings.Add("Checked", bs, "DontFragment");
            textBoxTTL.DataBindings.Add("Text", bs, "Ttl");
            textBoxTimeout.DataBindings.Add("Text", bs, "Timeout");
            textBoxRetries.DataBindings.Add("Text", bs, "Retries");
        }
    }
}
