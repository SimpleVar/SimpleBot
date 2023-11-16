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
      SuspendLayout();
      // 
      // listChatters
      // 
      listChatters.ItemHeight = 15;
      listChatters.Location = new Point(12, 63);
      listChatters.Name = "listChatters";
      listChatters.SelectionMode = SelectionMode.MultiExtended;
      listChatters.Size = new Size(196, 199);
      listChatters.TabIndex = 4;
      listChatters.SelectedIndexChanged += listChatters_SelectedIndexChanged;
      listChatters.KeyDown += listChatters_KeyDown;
      // 
      // labelChatters
      // 
      labelChatters.AutoSize = true;
      labelChatters.Location = new Point(12, 45);
      labelChatters.Name = "labelChatters";
      labelChatters.Size = new Size(54, 15);
      labelChatters.TabIndex = 1;
      labelChatters.Text = "Chatters:";
      // 
      // btnUpdateChatters
      // 
      btnUpdateChatters.Location = new Point(100, 41);
      btnUpdateChatters.Name = "btnUpdateChatters";
      btnUpdateChatters.Size = new Size(58, 23);
      btnUpdateChatters.TabIndex = 2;
      btnUpdateChatters.Text = "Update";
      btnUpdateChatters.UseVisualStyleBackColor = true;
      btnUpdateChatters.Click += Bot_UpdatedUsersInChat;
      // 
      // lblTwConnected
      // 
      lblTwConnected.AutoSize = true;
      lblTwConnected.ForeColor = SystemColors.GrayText;
      lblTwConnected.Location = new Point(12, 16);
      lblTwConnected.Name = "lblTwConnected";
      lblTwConnected.Size = new Size(113, 15);
      lblTwConnected.TabIndex = 0;
      lblTwConnected.Text = "Twitch connecting...";
      // 
      // dialogUserDataFolder
      // 
      dialogUserDataFolder.Description = "Choose where your user preferences and persistent data will be stored";
      // 
      // btnMassBan
      // 
      btnMassBan.Location = new Point(12, 297);
      btnMassBan.Name = "btnMassBan";
      btnMassBan.Size = new Size(122, 56);
      btnMassBan.TabIndex = 5;
      btnMassBan.Text = "Mass Ban from list";
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
      cbFreezeChattersList.CheckAlign = ContentAlignment.TopCenter;
      cbFreezeChattersList.Location = new Point(164, 30);
      cbFreezeChattersList.Name = "cbFreezeChattersList";
      cbFreezeChattersList.Size = new Size(44, 33);
      cbFreezeChattersList.TabIndex = 3;
      cbFreezeChattersList.Text = "Freeze";
      cbFreezeChattersList.UseVisualStyleBackColor = true;
      cbFreezeChattersList.CheckedChanged += cbFreezeChattersList_CheckedChanged;
      // 
      // btnBanKnownBotsInChat
      // 
      btnBanKnownBotsInChat.Location = new Point(12, 268);
      btnBanKnownBotsInChat.Name = "btnBanKnownBotsInChat";
      btnBanKnownBotsInChat.Size = new Size(196, 23);
      btnBanKnownBotsInChat.TabIndex = 6;
      btnBanKnownBotsInChat.Text = "Ban them bots";
      btnBanKnownBotsInChat.UseVisualStyleBackColor = true;
      btnBanKnownBotsInChat.Click += btnBanKnownBotsInChat_Click;
      // 
      // srv
      // 
      srv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      srv.BorderStyle = BorderStyle.FixedSingle;
      srv.Location = new Point(214, 30);
      srv.MinimumSize = new Size(589, 423);
      srv.Name = "srv";
      srv.Size = new Size(589, 423);
      srv.TabIndex = 7;
      // 
      // MainForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(803, 453);
      Controls.Add(srv);
      Controls.Add(btnBanKnownBotsInChat);
      Controls.Add(btnMassBan);
      Controls.Add(lblTwConnected);
      Controls.Add(labelChatters);
      Controls.Add(listChatters);
      Controls.Add(btnUpdateChatters);
      Controls.Add(cbFreezeChattersList);
      DoubleBuffered = true;
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
  }
}