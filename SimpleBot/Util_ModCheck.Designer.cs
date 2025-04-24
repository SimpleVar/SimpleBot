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
            txtName = new TextBox();
            txtAuthURL = new TextBox();
            label2 = new Label();
            btnGenAuth = new Button();
            label3 = new Label();
            btnGo = new Button();
            label4 = new Label();
            label5 = new Label();
            txtAllFollows = new TextBox();
            txtModChs = new TextBox();
            label6 = new Label();
            SuspendLayout();
            // 
            // txtName
            // 
            txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtName.Location = new Point(121, 12);
            txtName.Name = "txtName";
            txtName.Size = new Size(101, 23);
            txtName.TabIndex = 7;
            // 
            // txtAuthURL
            // 
            txtAuthURL.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtAuthURL.Location = new Point(12, 56);
            txtAuthURL.Name = "txtAuthURL";
            txtAuthURL.Size = new Size(317, 23);
            txtAuthURL.TabIndex = 8;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 15);
            label2.Name = "label2";
            label2.Size = new Size(103, 15);
            label2.TabIndex = 9;
            label2.Text = "Your twitch name:";
            // 
            // btnGenAuth
            // 
            btnGenAuth.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnGenAuth.Location = new Point(228, 12);
            btnGenAuth.Name = "btnGenAuth";
            btnGenAuth.Size = new Size(101, 23);
            btnGenAuth.TabIndex = 10;
            btnGenAuth.Text = "Generate Auth";
            btnGenAuth.UseVisualStyleBackColor = true;
            btnGenAuth.Click += btnGenAuth_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 38);
            label3.Name = "label3";
            label3.Size = new Size(224, 15);
            label3.TabIndex = 11;
            label3.Text = "Paste redirected URL from auth into here:";
            // 
            // btnGo
            // 
            btnGo.Location = new Point(254, 85);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(75, 23);
            btnGo.TabIndex = 12;
            btnGo.Text = "Go";
            btnGo.UseVisualStyleBackColor = true;
            btnGo.Click += btnGo_Click;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Location = new Point(44, 89);
            label4.Name = "label4";
            label4.Size = new Size(204, 15);
            label4.TabIndex = 13;
            label4.Text = "And then click this pretty button --->";
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label5.AutoSize = true;
            label5.Location = new Point(12, 133);
            label5.Name = "label5";
            label5.Size = new Size(65, 15);
            label5.TabIndex = 16;
            label5.Text = "All follows:";
            // 
            // txtAllFollows
            // 
            txtAllFollows.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtAllFollows.Location = new Point(12, 151);
            txtAllFollows.MaxLength = 2232767;
            txtAllFollows.Multiline = true;
            txtAllFollows.Name = "txtAllFollows";
            txtAllFollows.Size = new Size(317, 75);
            txtAllFollows.TabIndex = 17;
            // 
            // txtModChs
            // 
            txtModChs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtModChs.BackColor = SystemColors.HighlightText;
            txtModChs.Location = new Point(12, 247);
            txtModChs.MaxLength = 2232767;
            txtModChs.Multiline = true;
            txtModChs.Name = "txtModChs";
            txtModChs.ReadOnly = true;
            txtModChs.Size = new Size(317, 96);
            txtModChs.TabIndex = 19;
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label6.AutoSize = true;
            label6.Location = new Point(12, 229);
            label6.Name = "label6";
            label6.Size = new Size(177, 15);
            label6.TabIndex = 18;
            label6.Text = "Channels in which you are mod:";
            // 
            // Util_ModCheck
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(346, 355);
            Controls.Add(txtModChs);
            Controls.Add(label6);
            Controls.Add(txtAllFollows);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(btnGo);
            Controls.Add(label3);
            Controls.Add(btnGenAuth);
            Controls.Add(label2);
            Controls.Add(txtAuthURL);
            Controls.Add(txtName);
            Name = "Util_ModCheck";
            Text = "Twitch mod list thing";
            ResumeLayout(false);
            PerformLayout();
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