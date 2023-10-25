namespace SimpleBot
{
  partial class SongRequestView
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      btnShowYoutubeForm = new Button();
      label1 = new Label();
      nudMinSeconds = new NumericUpDown();
      label2 = new Label();
      nudMaxSeconds = new NumericUpDown();
      label3 = new Label();
      nudMaxPerUser = new NumericUpDown();
      sliderVolume = new TrackBar();
      label6 = new Label();
      labelVolume = new Label();
      label4 = new Label();
      nudMaxVolume = new NumericUpDown();
      btnImportPlaylist = new Button();
      btnExportPlaylist = new Button();
      sfd = new SaveFileDialog();
      ofd = new OpenFileDialog();
      btnSkip = new Button();
      lblCurrSong = new Label();
      ((System.ComponentModel.ISupportInitialize)nudMinSeconds).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxSeconds).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxPerUser).BeginInit();
      ((System.ComponentModel.ISupportInitialize)sliderVolume).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxVolume).BeginInit();
      SuspendLayout();
      // 
      // btnShowYoutubeForm
      // 
      btnShowYoutubeForm.Location = new Point(3, 3);
      btnShowYoutubeForm.Name = "btnShowYoutubeForm";
      btnShowYoutubeForm.Size = new Size(115, 23);
      btnShowYoutubeForm.TabIndex = 0;
      btnShowYoutubeForm.Text = "YouTube Player";
      btnShowYoutubeForm.UseVisualStyleBackColor = true;
      btnShowYoutubeForm.Click += btnShowYoutubeForm_Click;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(7, 34);
      label1.Name = "label1";
      label1.Size = new Size(130, 15);
      label1.TabIndex = 1;
      label1.Text = "Min duration (seconds)";
      // 
      // nudMinSeconds
      // 
      nudMinSeconds.Location = new Point(145, 32);
      nudMinSeconds.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
      nudMinSeconds.Name = "nudMinSeconds";
      nudMinSeconds.Size = new Size(64, 23);
      nudMinSeconds.TabIndex = 2;
      nudMinSeconds.ValueChanged += nudMinSeconds_ValueChanged;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(7, 63);
      label2.Name = "label2";
      label2.Size = new Size(132, 15);
      label2.TabIndex = 3;
      label2.Text = "Max duration (seconds)";
      // 
      // nudMaxSeconds
      // 
      nudMaxSeconds.Location = new Point(145, 61);
      nudMaxSeconds.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
      nudMaxSeconds.Name = "nudMaxSeconds";
      nudMaxSeconds.Size = new Size(64, 23);
      nudMaxSeconds.TabIndex = 4;
      nudMaxSeconds.ValueChanged += nudMaxSeconds_ValueChanged;
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point(7, 92);
      label3.Name = "label3";
      label3.Size = new Size(120, 15);
      label3.TabIndex = 5;
      label3.Text = "Limit queues per user";
      // 
      // nudMaxPerUser
      // 
      nudMaxPerUser.Location = new Point(145, 90);
      nudMaxPerUser.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
      nudMaxPerUser.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxPerUser.Name = "nudMaxPerUser";
      nudMaxPerUser.Size = new Size(64, 23);
      nudMaxPerUser.TabIndex = 6;
      nudMaxPerUser.Value = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxPerUser.ValueChanged += nudMaxPerUser_ValueChanged;
      // 
      // sliderVolume
      // 
      sliderVolume.Location = new Point(56, 148);
      sliderVolume.Maximum = 100;
      sliderVolume.Name = "sliderVolume";
      sliderVolume.Size = new Size(180, 45);
      sliderVolume.SmallChange = 2;
      sliderVolume.TabIndex = 10;
      sliderVolume.TickFrequency = 10;
      sliderVolume.Scroll += sliderVolume_Scroll;
      // 
      // label6
      // 
      label6.AutoSize = true;
      label6.Location = new Point(7, 150);
      label6.Name = "label6";
      label6.Size = new Size(47, 15);
      label6.TabIndex = 9;
      label6.Text = "Volume";
      // 
      // labelVolume
      // 
      labelVolume.AutoSize = true;
      labelVolume.Location = new Point(234, 150);
      labelVolume.Name = "labelVolume";
      labelVolume.Size = new Size(25, 15);
      labelVolume.TabIndex = 11;
      labelVolume.Text = "100";
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(7, 121);
      label4.Name = "label4";
      label4.Size = new Size(117, 15);
      label4.TabIndex = 7;
      label4.Text = "Limit volume (0-100)";
      // 
      // nudMaxVolume
      // 
      nudMaxVolume.Location = new Point(145, 119);
      nudMaxVolume.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxVolume.Name = "nudMaxVolume";
      nudMaxVolume.Size = new Size(64, 23);
      nudMaxVolume.TabIndex = 8;
      nudMaxVolume.Value = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxVolume.ValueChanged += nudMaxVolume_ValueChanged;
      // 
      // btnImportPlaylist
      // 
      btnImportPlaylist.Location = new Point(229, 3);
      btnImportPlaylist.Name = "btnImportPlaylist";
      btnImportPlaylist.Size = new Size(99, 23);
      btnImportPlaylist.TabIndex = 12;
      btnImportPlaylist.Text = "Import Playlist";
      btnImportPlaylist.UseVisualStyleBackColor = true;
      btnImportPlaylist.Click += btnImportPlaylist_Click;
      // 
      // btnExportPlaylist
      // 
      btnExportPlaylist.Location = new Point(229, 26);
      btnExportPlaylist.Name = "btnExportPlaylist";
      btnExportPlaylist.Size = new Size(99, 23);
      btnExportPlaylist.TabIndex = 12;
      btnExportPlaylist.Text = "Export Playlist";
      btnExportPlaylist.UseVisualStyleBackColor = true;
      btnExportPlaylist.Click += btnExportPlaylist_Click;
      // 
      // sfd
      // 
      sfd.Filter = "Text Files|*.txt";
      // 
      // ofd
      // 
      ofd.Filter = "Text Files|*.txt";
      // 
      // btnSkip
      // 
      btnSkip.Location = new Point(124, 3);
      btnSkip.Name = "btnSkip";
      btnSkip.Size = new Size(48, 23);
      btnSkip.TabIndex = 13;
      btnSkip.Text = "Skip";
      btnSkip.UseVisualStyleBackColor = true;
      btnSkip.Click += btnSkip_Click;
      // 
      // lblCurrSong
      // 
      lblCurrSong.AutoSize = true;
      lblCurrSong.Location = new Point(7, 196);
      lblCurrSong.Name = "lblCurrSong";
      lblCurrSong.Size = new Size(37, 15);
      lblCurrSong.TabIndex = 14;
      lblCurrSong.Text = "Song:";
      // 
      // SongRequestView
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(lblCurrSong);
      Controls.Add(btnSkip);
      Controls.Add(btnExportPlaylist);
      Controls.Add(btnImportPlaylist);
      Controls.Add(labelVolume);
      Controls.Add(sliderVolume);
      Controls.Add(label6);
      Controls.Add(nudMaxVolume);
      Controls.Add(label4);
      Controls.Add(nudMaxPerUser);
      Controls.Add(label3);
      Controls.Add(nudMaxSeconds);
      Controls.Add(label2);
      Controls.Add(nudMinSeconds);
      Controls.Add(label1);
      Controls.Add(btnShowYoutubeForm);
      Name = "SongRequestView";
      Size = new Size(331, 252);
      ((System.ComponentModel.ISupportInitialize)nudMinSeconds).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxSeconds).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxPerUser).EndInit();
      ((System.ComponentModel.ISupportInitialize)sliderVolume).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxVolume).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Button btnShowYoutubeForm;
    private Label label1;
    private NumericUpDown nudMinSeconds;
    private Label label2;
    private NumericUpDown nudMaxSeconds;
    private Label label3;
    private NumericUpDown nudMaxPerUser;
    private TrackBar sliderVolume;
    private Label label6;
    private Label labelVolume;
    private Label label4;
    private NumericUpDown nudMaxVolume;
    private Button btnImportPlaylist;
    private Button btnExportPlaylist;
    private SaveFileDialog sfd;
    private OpenFileDialog ofd;
    private Button btnSkip;
    private Label lblCurrSong;
  }
}
