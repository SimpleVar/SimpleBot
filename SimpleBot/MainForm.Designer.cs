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
      SuspendLayout();
      // 
      // listChatters
      // 
      listChatters.ItemHeight = 15;
      listChatters.Location = new Point(12, 63);
      listChatters.Name = "listChatters";
      listChatters.SelectionMode = SelectionMode.MultiExtended;
      listChatters.Size = new Size(169, 244);
      listChatters.TabIndex = 6;
      // 
      // labelChatters
      // 
      labelChatters.AutoSize = true;
      labelChatters.Location = new Point(12, 45);
      labelChatters.Name = "labelChatters";
      labelChatters.Size = new Size(54, 15);
      labelChatters.TabIndex = 4;
      labelChatters.Text = "Chatters:";
      // 
      // btnUpdateChatters
      // 
      btnUpdateChatters.Location = new Point(123, 41);
      btnUpdateChatters.Name = "btnUpdateChatters";
      btnUpdateChatters.Size = new Size(58, 23);
      btnUpdateChatters.TabIndex = 5;
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
      btnMassBan.Location = new Point(286, 89);
      btnMassBan.Name = "btnMassBan";
      btnMassBan.Size = new Size(337, 59);
      btnMassBan.TabIndex = 7;
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
      // MainForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(674, 355);
      Controls.Add(btnMassBan);
      Controls.Add(lblTwConnected);
      Controls.Add(labelChatters);
      Controls.Add(listChatters);
      Controls.Add(btnUpdateChatters);
      Name = "MainForm";
      Text = "SimpleBot";
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
  }
}