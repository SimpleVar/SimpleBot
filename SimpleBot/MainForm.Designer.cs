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
            listChatters = new ListBox();
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
            SuspendLayout();
            // 
            // listChatters
            // 
            listChatters.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listChatters.IntegralHeight = false;
            listChatters.ItemHeight = 15;
            listChatters.Location = new Point(0, 48);
            listChatters.Name = "listChatters";
            listChatters.SelectionMode = SelectionMode.MultiExtended;
            listChatters.Size = new Size(196, 168);
            listChatters.TabIndex = 5;
            listChatters.SelectedIndexChanged += listChatters_SelectedIndexChanged;
            listChatters.KeyDown += listChatters_KeyDown;
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
            lblTwConnected.Size = new Size(113, 15);
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
            btnMassBan.Location = new Point(-1, 239);
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
            btnBanKnownBotsInChat.Location = new Point(-1, 214);
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
            listRecentFollows.Location = new Point(0, 302);
            listRecentFollows.Name = "listRecentFollows";
            listRecentFollows.SelectionMode = SelectionMode.MultiExtended;
            listRecentFollows.Size = new Size(196, 79);
            listRecentFollows.TabIndex = 9;
            // 
            // btnBanSelectedFollows
            // 
            btnBanSelectedFollows.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnBanSelectedFollows.Location = new Point(-1, 281);
            btnBanSelectedFollows.Name = "btnBanSelectedFollows";
            btnBanSelectedFollows.Size = new Size(198, 23);
            btnBanSelectedFollows.TabIndex = 8;
            btnBanSelectedFollows.Text = "Ban selected recent follows:";
            btnBanSelectedFollows.UseVisualStyleBackColor = true;
            btnBanSelectedFollows.Click += btnBanSelectedFollows_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(534, 381);
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
            MinimumSize = new Size(511, 299);
            Name = "MainForm";
            Text = "SimpleBot";
            Load += MainForm_Load;
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
    private SongRequestView srv;
    private ListBox listRecentFollows;
    private Button btnBanSelectedFollows;
  }
}