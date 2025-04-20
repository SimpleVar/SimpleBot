namespace SimpleBot
{
  partial class MainForm
  {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            listChatters = new ListBox();
            ctxChatters = new ContextMenuStrip(components);
            toolStripMenuItem1 = new ToolStripMenuItem();
            labelChatters = new Label();
            btnUpdateChatters = new Button();
            lblTwConnected = new Label();
            dialogUserDataFolder = new FolderBrowserDialog();
            btnMassBan = new Button();
            ofd = new OpenFileDialog();
            cbFreezeChattersList = new CheckBox();
            btnBanKnownBotsInChat = new Button();
            srv = new SongRequestView();
            listRecentFollows = new ListBox();
            btnBanSelectedFollows = new Button();
            colorDialog = new ColorDialog();
            txtDbg = new TextBox();
            ctxChatters.SuspendLayout();
            SuspendLayout();
            // 
            // listChatters
            // 
            listChatters.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listChatters.ContextMenuStrip = ctxChatters;
            listChatters.IntegralHeight = false;
            listChatters.ItemHeight = 15;
            listChatters.Location = new Point(0, 48);
            listChatters.Name = "listChatters";
            listChatters.SelectionMode = SelectionMode.MultiExtended;
            listChatters.Size = new Size(196, 128);
            listChatters.TabIndex = 5;
            listChatters.SelectedIndexChanged += listChatters_SelectedIndexChanged;
            listChatters.KeyDown += listChatters_KeyDown;
            // 
            // ctxChatters
            // 
            ctxChatters.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            ctxChatters.Name = "ctxChatters";
            ctxChatters.ShowImageMargin = false;
            ctxChatters.Size = new Size(98, 26);
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(97, 22);
            toolStripMenuItem1.Text = "Set Color";
            toolStripMenuItem1.Click += toolStripMenuItem1_Click;
            // 
            // labelChatters
            // 
            labelChatters.AutoSize = true;
            labelChatters.Location = new Point(-1, 30);
            labelChatters.Name = "labelChatters";
            labelChatters.Size = new Size(54, 15);
            labelChatters.TabIndex = 2;
            labelChatters.Text = "Chatters:";
            // 
            // btnUpdateChatters
            // 
            btnUpdateChatters.Location = new Point(138, 27);
            btnUpdateChatters.Name = "btnUpdateChatters";
            btnUpdateChatters.Size = new Size(58, 23);
            btnUpdateChatters.TabIndex = 4;
            btnUpdateChatters.Text = "Update";
            btnUpdateChatters.UseVisualStyleBackColor = true;
            btnUpdateChatters.Click += Bot_UpdatedUsersInChat;
            // 
            // lblTwConnected
            // 
            lblTwConnected.AutoSize = true;
            lblTwConnected.ForeColor = SystemColors.GrayText;
            lblTwConnected.Location = new Point(0, 8);
            lblTwConnected.Name = "lblTwConnected";
            lblTwConnected.Size = new Size(114, 15);
            lblTwConnected.TabIndex = 1;
            lblTwConnected.Text = "Twitch connecting...";
            // 
            // dialogUserDataFolder
            // 
            dialogUserDataFolder.Description = "Choose where your user preferences and persistent data will be stored";
            // 
            // btnMassBan
            // 
            btnMassBan.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnMassBan.Location = new Point(-1, 199);
            btnMassBan.Name = "btnMassBan";
            btnMassBan.Size = new Size(198, 40);
            btnMassBan.TabIndex = 7;
            btnMassBan.Text = "Mass ban from list";
            btnMassBan.UseVisualStyleBackColor = true;
            btnMassBan.Click += btnMassBan_Click;
            // 
            // ofd
            // 
            ofd.AddToRecent = false;
            ofd.Filter = "Text files|*.txt|All files|*.*";
            ofd.SupportMultiDottedExtensions = true;
            // 
            // cbFreezeChattersList
            // 
            cbFreezeChattersList.AutoSize = true;
            cbFreezeChattersList.Location = new Point(78, 29);
            cbFreezeChattersList.Name = "cbFreezeChattersList";
            cbFreezeChattersList.Size = new Size(59, 19);
            cbFreezeChattersList.TabIndex = 3;
            cbFreezeChattersList.Text = "Freeze";
            cbFreezeChattersList.UseVisualStyleBackColor = true;
            cbFreezeChattersList.CheckedChanged += cbFreezeChattersList_CheckedChanged;
            // 
            // btnBanKnownBotsInChat
            // 
            btnBanKnownBotsInChat.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnBanKnownBotsInChat.Location = new Point(-1, 174);
            btnBanKnownBotsInChat.Name = "btnBanKnownBotsInChat";
            btnBanKnownBotsInChat.Size = new Size(198, 23);
            btnBanKnownBotsInChat.TabIndex = 6;
            btnBanKnownBotsInChat.Text = "Ban them bots";
            btnBanKnownBotsInChat.UseVisualStyleBackColor = true;
            btnBanKnownBotsInChat.Click += btnBanKnownBotsInChat_Click;
            // 
            // srv
            // 
            srv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            srv.BorderStyle = BorderStyle.FixedSingle;
            srv.Location = new Point(195, 0);
            srv.MinimumSize = new Size(300, 310);
            srv.Name = "srv";
            srv.Size = new Size(340, 381);
            srv.TabIndex = 0;
            // 
            // listRecentFollows
            // 
            listRecentFollows.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            listRecentFollows.FormattingEnabled = true;
            listRecentFollows.ItemHeight = 15;
            listRecentFollows.Location = new Point(0, 262);
            listRecentFollows.Margin = new Padding(0);
            listRecentFollows.Name = "listRecentFollows";
            listRecentFollows.SelectionMode = SelectionMode.MultiExtended;
            listRecentFollows.Size = new Size(196, 34);
            listRecentFollows.TabIndex = 9;
            // 
            // btnBanSelectedFollows
            // 
            btnBanSelectedFollows.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnBanSelectedFollows.Location = new Point(-1, 241);
            btnBanSelectedFollows.Name = "btnBanSelectedFollows";
            btnBanSelectedFollows.Size = new Size(198, 23);
            btnBanSelectedFollows.TabIndex = 8;
            btnBanSelectedFollows.Text = "Ban selected recent follows:";
            btnBanSelectedFollows.UseVisualStyleBackColor = true;
            btnBanSelectedFollows.Click += btnBanSelectedFollows_Click;
            // 
            // colorDialog
            // 
            colorDialog.AnyColor = true;
            colorDialog.SolidColorOnly = true;
            // 
            // txtDbg
            // 
            txtDbg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            txtDbg.BorderStyle = BorderStyle.FixedSingle;
            txtDbg.Location = new Point(0, 296);
            txtDbg.Margin = new Padding(0);
            txtDbg.MaxLength = 3276700;
            txtDbg.Multiline = true;
            txtDbg.Name = "txtDbg";
            txtDbg.Size = new Size(196, 85);
            txtDbg.TabIndex = 10;
            txtDbg.WordWrap = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(534, 381);
            Controls.Add(txtDbg);
            Controls.Add(srv);
            Controls.Add(listRecentFollows);
            Controls.Add(btnBanSelectedFollows);
            Controls.Add(btnMassBan);
            Controls.Add(lblTwConnected);
            Controls.Add(labelChatters);
            Controls.Add(listChatters);
            Controls.Add(btnUpdateChatters);
            Controls.Add(cbFreezeChattersList);
            Controls.Add(btnBanKnownBotsInChat);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(511, 299);
            Name = "MainForm";
            Text = "SimpleBot";
            Load += MainForm_Load;
            ctxChatters.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox listChatters;
    private Label labelChatters;
    private Button btnUpdateChatters;
    private Label lblTwConnected;
    private FolderBrowserDialog dialogUserDataFolder;
    private Button btnMassBan;
    private OpenFileDialog ofd;
    private CheckBox cbFreezeChattersList;
    private Button btnBanKnownBotsInChat;
    private ListBox listRecentFollows;
    private Button btnBanSelectedFollows;
        private ColorDialog colorDialog;
        private ContextMenuStrip ctxChatters;
        private ToolStripMenuItem toolStripMenuItem1;
        private TextBox txtDbg;
        public SongRequestView srv;
    }
}