namespace SimpleBot
{
  partial class Util_ModCheck
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
      this.txtName = new System.Windows.Forms.TextBox();
      this.txtAuthURL = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.btnGenAuth = new System.Windows.Forms.Button();
      this.label3 = new System.Windows.Forms.Label();
      this.btnGo = new System.Windows.Forms.Button();
      this.label4 = new System.Windows.Forms.Label();
      this.label5 = new System.Windows.Forms.Label();
      this.txtAllFollows = new System.Windows.Forms.TextBox();
      this.txtModChs = new System.Windows.Forms.TextBox();
      this.label6 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // txtName
      // 
      this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtName.Location = new System.Drawing.Point(121, 12);
      this.txtName.Name = "txtName";
      this.txtName.Size = new System.Drawing.Size(101, 23);
      this.txtName.TabIndex = 7;
      // 
      // txtAuthURL
      // 
      this.txtAuthURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtAuthURL.Location = new System.Drawing.Point(12, 56);
      this.txtAuthURL.Name = "txtAuthURL";
      this.txtAuthURL.Size = new System.Drawing.Size(317, 23);
      this.txtAuthURL.TabIndex = 8;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(12, 15);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(103, 15);
      this.label2.TabIndex = 9;
      this.label2.Text = "Your twitch name:";
      // 
      // btnGenAuth
      // 
      this.btnGenAuth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnGenAuth.Location = new System.Drawing.Point(228, 12);
      this.btnGenAuth.Name = "btnGenAuth";
      this.btnGenAuth.Size = new System.Drawing.Size(101, 23);
      this.btnGenAuth.TabIndex = 10;
      this.btnGenAuth.Text = "Generate Auth";
      this.btnGenAuth.UseVisualStyleBackColor = true;
      this.btnGenAuth.Click += new System.EventHandler(this.btnGenAuth_Click);
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(12, 38);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(224, 15);
      this.label3.TabIndex = 11;
      this.label3.Text = "Paste redirected URL from auth into here:";
      // 
      // btnGo
      // 
      this.btnGo.Location = new System.Drawing.Point(254, 85);
      this.btnGo.Name = "btnGo";
      this.btnGo.Size = new System.Drawing.Size(75, 23);
      this.btnGo.TabIndex = 12;
      this.btnGo.Text = "Go";
      this.btnGo.UseVisualStyleBackColor = true;
      this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
      // 
      // label4
      // 
      this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(44, 89);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(204, 15);
      this.label4.TabIndex = 13;
      this.label4.Text = "And then click this pretty button --->";
      // 
      // label5
      // 
      this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(12, 133);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(65, 15);
      this.label5.TabIndex = 16;
      this.label5.Text = "All follows:";
      // 
      // txtAllFollows
      // 
      this.txtAllFollows.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtAllFollows.Location = new System.Drawing.Point(12, 151);
      this.txtAllFollows.MaxLength = 2232767;
      this.txtAllFollows.Multiline = true;
      this.txtAllFollows.Name = "txtAllFollows";
      this.txtAllFollows.Size = new System.Drawing.Size(317, 75);
      this.txtAllFollows.TabIndex = 17;
      // 
      // txtModChs
      // 
      this.txtModChs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtModChs.BackColor = System.Drawing.SystemColors.HighlightText;
      this.txtModChs.Location = new System.Drawing.Point(12, 247);
      this.txtModChs.MaxLength = 2232767;
      this.txtModChs.Multiline = true;
      this.txtModChs.Name = "txtModChs";
      this.txtModChs.ReadOnly = true;
      this.txtModChs.Size = new System.Drawing.Size(317, 96);
      this.txtModChs.TabIndex = 19;
      // 
      // label6
      // 
      this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(12, 229);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(177, 15);
      this.label6.TabIndex = 18;
      this.label6.Text = "Channels in which you are mod:";
      // 
      // Util_ModCheck
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(346, 355);
      this.Controls.Add(this.txtModChs);
      this.Controls.Add(this.label6);
      this.Controls.Add(this.txtAllFollows);
      this.Controls.Add(this.label5);
      this.Controls.Add(this.label4);
      this.Controls.Add(this.btnGo);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.btnGenAuth);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.txtAuthURL);
      this.Controls.Add(this.txtName);
      this.Name = "Util_ModCheck";
      this.Text = "Twitch mod list thing";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion
    private TextBox txtName;
    private TextBox txtAuthURL;
    private Label label2;
    private Button btnGenAuth;
    private Label label3;
    private Button btnGo;
    private Label label4;
    private Label label5;
    private TextBox txtAllFollows;
    private TextBox txtModChs;
    private Label label6;
  }
}