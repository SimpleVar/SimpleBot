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
      this.listChatters = new System.Windows.Forms.ListBox();
      this.label1 = new System.Windows.Forms.Label();
      this.btnUpdateChatters = new System.Windows.Forms.Button();
      this.lblTwConnected = new System.Windows.Forms.Label();
      this.dialogUserDataFolder = new System.Windows.Forms.FolderBrowserDialog();
      this.btnMassBan = new System.Windows.Forms.Button();
      this.ofd = new System.Windows.Forms.OpenFileDialog();
      this.SuspendLayout();
      // 
      // listChatters
      // 
      this.listChatters.ItemHeight = 15;
      this.listChatters.Location = new System.Drawing.Point(12, 63);
      this.listChatters.Name = "listChatters";
      this.listChatters.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
      this.listChatters.Size = new System.Drawing.Size(169, 244);
      this.listChatters.TabIndex = 6;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 45);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(54, 15);
      this.label1.TabIndex = 4;
      this.label1.Text = "Chatters:";
      // 
      // btnUpdateChatters
      // 
      this.btnUpdateChatters.Location = new System.Drawing.Point(123, 41);
      this.btnUpdateChatters.Name = "btnUpdateChatters";
      this.btnUpdateChatters.Size = new System.Drawing.Size(58, 23);
      this.btnUpdateChatters.TabIndex = 5;
      this.btnUpdateChatters.Text = "Update";
      this.btnUpdateChatters.UseVisualStyleBackColor = true;
      this.btnUpdateChatters.Click += new System.EventHandler(this.Bot_UpdatedUsersInChat);
      // 
      // lblTwConnected
      // 
      this.lblTwConnected.AutoSize = true;
      this.lblTwConnected.ForeColor = System.Drawing.SystemColors.GrayText;
      this.lblTwConnected.Location = new System.Drawing.Point(12, 16);
      this.lblTwConnected.Name = "lblTwConnected";
      this.lblTwConnected.Size = new System.Drawing.Size(113, 15);
      this.lblTwConnected.TabIndex = 0;
      this.lblTwConnected.Text = "Twitch connecting...";
      // 
      // dialogUserDataFolder
      // 
      this.dialogUserDataFolder.Description = "Choose where your user preferences and persistent data will be stored";
      // 
      // btnMassBan
      // 
      this.btnMassBan.Location = new System.Drawing.Point(286, 89);
      this.btnMassBan.Name = "btnMassBan";
      this.btnMassBan.Size = new System.Drawing.Size(337, 59);
      this.btnMassBan.TabIndex = 7;
      this.btnMassBan.Text = "Mass Ban from list";
      this.btnMassBan.UseVisualStyleBackColor = true;
      this.btnMassBan.Click += new System.EventHandler(this.btnMassBan_Click);
      // 
      // ofd
      // 
      this.ofd.AddToRecent = false;
      this.ofd.Filter = "Text files|*.txt|All files|*.*";
      this.ofd.SupportMultiDottedExtensions = true;
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(674, 355);
      this.Controls.Add(this.btnMassBan);
      this.Controls.Add(this.lblTwConnected);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.listChatters);
      this.Controls.Add(this.btnUpdateChatters);
      this.Name = "MainForm";
      this.Text = "SimpleBot";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private ListBox listChatters;
    private Label label1;
    private Button btnUpdateChatters;
    private Label lblTwConnected;
    private FolderBrowserDialog dialogUserDataFolder;
    private Button btnMassBan;
    private OpenFileDialog ofd;
  }
}