namespace WinClient
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxServer = new System.Windows.Forms.TextBox();
            this.textBoxLocalFilename = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxRemoteFilename = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.comboBoxOperation = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.textBoxRemoteBaseDirectory = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBoxAutoGenerateRemoteFilename = new System.Windows.Forms.CheckBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server";
            // 
            // textBoxServer
            // 
            this.textBoxServer.Location = new System.Drawing.Point(169, 15);
            this.textBoxServer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxServer.Name = "textBoxServer";
            this.textBoxServer.Size = new System.Drawing.Size(401, 22);
            this.textBoxServer.TabIndex = 1;
            this.textBoxServer.Text = "127.0.0.1:69";
            // 
            // textBoxLocalFilename
            // 
            this.textBoxLocalFilename.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBoxLocalFilename.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.textBoxLocalFilename.Location = new System.Drawing.Point(169, 170);
            this.textBoxLocalFilename.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxLocalFilename.Name = "textBoxLocalFilename";
            this.textBoxLocalFilename.Size = new System.Drawing.Size(401, 22);
            this.textBoxLocalFilename.TabIndex = 10;
            this.textBoxLocalFilename.TextChanged += new System.EventHandler(this.textBoxLocalFilename_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 174);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 17);
            this.label2.TabIndex = 9;
            this.label2.Text = "Local filename";
            // 
            // textBoxRemoteFilename
            // 
            this.textBoxRemoteFilename.Location = new System.Drawing.Point(169, 138);
            this.textBoxRemoteFilename.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxRemoteFilename.Name = "textBoxRemoteFilename";
            this.textBoxRemoteFilename.Size = new System.Drawing.Size(401, 22);
            this.textBoxRemoteFilename.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 142);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(114, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Remote filename";
            // 
            // buttonSettings
            // 
            this.buttonSettings.Location = new System.Drawing.Point(648, 15);
            this.buttonSettings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(143, 52);
            this.buttonSettings.TabIndex = 12;
            this.buttonSettings.Text = "&Settings";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar1,
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 212);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip1.Size = new System.Drawing.Size(807, 26);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 14;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(200, 20);
            this.toolStripProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(13, 21);
            this.toolStripStatusLabel1.Text = " ";
            // 
            // comboBoxOperation
            // 
            this.comboBoxOperation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOperation.FormattingEnabled = true;
            this.comboBoxOperation.Items.AddRange(new object[] {
            "Download",
            "Upload"});
            this.comboBoxOperation.Location = new System.Drawing.Point(169, 47);
            this.comboBoxOperation.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBoxOperation.Name = "comboBoxOperation";
            this.comboBoxOperation.Size = new System.Drawing.Size(160, 24);
            this.comboBoxOperation.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 50);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 17);
            this.label4.TabIndex = 2;
            this.label4.Text = "Operation";
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(651, 138);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(143, 57);
            this.buttonStart.TabIndex = 13;
            this.buttonStart.Text = "&Go";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(584, 167);
            this.button3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(41, 30);
            this.button3.TabIndex = 11;
            this.button3.Text = "...";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // textBoxRemoteBaseDirectory
            // 
            this.textBoxRemoteBaseDirectory.Location = new System.Drawing.Point(169, 106);
            this.textBoxRemoteBaseDirectory.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxRemoteBaseDirectory.Name = "textBoxRemoteBaseDirectory";
            this.textBoxRemoteBaseDirectory.Size = new System.Drawing.Size(401, 22);
            this.textBoxRemoteBaseDirectory.TabIndex = 6;
            this.textBoxRemoteBaseDirectory.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 110);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(151, 17);
            this.label5.TabIndex = 5;
            this.label5.Text = "Remote base directory";
            // 
            // checkBoxAutoGenerateRemoteFilename
            // 
            this.checkBoxAutoGenerateRemoteFilename.AutoSize = true;
            this.checkBoxAutoGenerateRemoteFilename.Checked = true;
            this.checkBoxAutoGenerateRemoteFilename.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoGenerateRemoteFilename.Location = new System.Drawing.Point(169, 80);
            this.checkBoxAutoGenerateRemoteFilename.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxAutoGenerateRemoteFilename.Name = "checkBoxAutoGenerateRemoteFilename";
            this.checkBoxAutoGenerateRemoteFilename.Size = new System.Drawing.Size(279, 21);
            this.checkBoxAutoGenerateRemoteFilename.TabIndex = 4;
            this.checkBoxAutoGenerateRemoteFilename.Text = "Automatically generate remote filename";
            this.checkBoxAutoGenerateRemoteFilename.UseVisualStyleBackColor = true;
            this.checkBoxAutoGenerateRemoteFilename.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.checkBoxAutoGenerateRemoteFilename);
            this.panel1.Controls.Add(this.comboBoxOperation);
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.textBoxRemoteBaseDirectory);
            this.panel1.Controls.Add(this.textBoxRemoteFilename);
            this.panel1.Controls.Add(this.textBoxLocalFilename);
            this.panel1.Controls.Add(this.textBoxServer);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(644, 209);
            this.panel1.TabIndex = 15;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 238);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FormMain";
            this.Text = "TFTP Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxServer;
        private System.Windows.Forms.TextBox textBoxLocalFilename;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxRemoteFilename;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ComboBox comboBoxOperation;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.TextBox textBoxRemoteBaseDirectory;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox checkBoxAutoGenerateRemoteFilename;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.Panel panel1;
    }
}

