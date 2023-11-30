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
      btnPrev = new Button();
      btnSaveCurrToPlaylist = new Button();
      btnSavePrevToPlaylist = new Button();
      btnTogglePlayPause = new Button();
      lblQueueSize = new Label();
      lblPlaylist = new Label();
      btnRemoveCurrFromPlaylist = new Button();
      dgvQueueAndPlaylist = new DataGridView();
      Index = new DataGridViewTextBoxColumn();
      Title = new DataGridViewTextBoxColumn();
      Author = new DataGridViewTextBoxColumn();
      Duration = new DataGridViewLinkColumn();
      RequestedBy = new DataGridViewTextBoxColumn();
      VideoId = new DataGridViewTextBoxColumn();
      panel1 = new Panel();
      cbIsSearchRegex = new CheckBox();
      txtSearch = new TextBox();
      panelSettings = new Panel();
      panel4 = new Panel();
      btnShowHideSettings = new Button();
      ((System.ComponentModel.ISupportInitialize)nudMinSeconds).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxSeconds).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxPerUser).BeginInit();
      ((System.ComponentModel.ISupportInitialize)sliderVolume).BeginInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxVolume).BeginInit();
      ((System.ComponentModel.ISupportInitialize)dgvQueueAndPlaylist).BeginInit();
      panel1.SuspendLayout();
      panelSettings.SuspendLayout();
      panel4.SuspendLayout();
      SuspendLayout();
      // 
      // btnShowYoutubeForm
      // 
      btnShowYoutubeForm.Location = new Point(114, 3);
      btnShowYoutubeForm.Name = "btnShowYoutubeForm";
      btnShowYoutubeForm.Size = new Size(107, 23);
      btnShowYoutubeForm.TabIndex = 1;
      btnShowYoutubeForm.Text = "YouTube Player";
      btnShowYoutubeForm.UseVisualStyleBackColor = true;
      btnShowYoutubeForm.Click += btnShowYoutubeForm_Click;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(3, 4);
      label1.Name = "label1";
      label1.Size = new Size(130, 15);
      label1.TabIndex = 0;
      label1.Text = "Min duration (seconds)";
      // 
      // nudMinSeconds
      // 
      nudMinSeconds.Location = new Point(141, 2);
      nudMinSeconds.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
      nudMinSeconds.Name = "nudMinSeconds";
      nudMinSeconds.Size = new Size(64, 23);
      nudMinSeconds.TabIndex = 1;
      nudMinSeconds.ValueChanged += nudMinSeconds_ValueChanged;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(3, 33);
      label2.Name = "label2";
      label2.Size = new Size(132, 15);
      label2.TabIndex = 2;
      label2.Text = "Max duration (seconds)";
      // 
      // nudMaxSeconds
      // 
      nudMaxSeconds.Location = new Point(141, 31);
      nudMaxSeconds.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
      nudMaxSeconds.Name = "nudMaxSeconds";
      nudMaxSeconds.Size = new Size(64, 23);
      nudMaxSeconds.TabIndex = 3;
      nudMaxSeconds.ValueChanged += nudMaxSeconds_ValueChanged;
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point(3, 62);
      label3.Name = "label3";
      label3.Size = new Size(120, 15);
      label3.TabIndex = 4;
      label3.Text = "Limit queues per user";
      // 
      // nudMaxPerUser
      // 
      nudMaxPerUser.Location = new Point(141, 60);
      nudMaxPerUser.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
      nudMaxPerUser.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxPerUser.Name = "nudMaxPerUser";
      nudMaxPerUser.Size = new Size(64, 23);
      nudMaxPerUser.TabIndex = 5;
      nudMaxPerUser.Value = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxPerUser.ValueChanged += nudMaxPerUser_ValueChanged;
      // 
      // sliderVolume
      // 
      sliderVolume.Location = new Point(54, 50);
      sliderVolume.Maximum = 100;
      sliderVolume.Name = "sliderVolume";
      sliderVolume.Size = new Size(180, 45);
      sliderVolume.SmallChange = 2;
      sliderVolume.TabIndex = 19;
      sliderVolume.TickFrequency = 10;
      sliderVolume.Scroll += sliderVolume_Scroll;
      // 
      // label6
      // 
      label6.AutoSize = true;
      label6.Location = new Point(5, 52);
      label6.Name = "label6";
      label6.Size = new Size(47, 15);
      label6.TabIndex = 18;
      label6.Text = "Volume";
      // 
      // labelVolume
      // 
      labelVolume.AutoSize = true;
      labelVolume.Location = new Point(240, 52);
      labelVolume.Name = "labelVolume";
      labelVolume.Size = new Size(25, 15);
      labelVolume.TabIndex = 20;
      labelVolume.Text = "100";
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(3, 91);
      label4.Name = "label4";
      label4.Size = new Size(117, 15);
      label4.TabIndex = 6;
      label4.Text = "Limit volume (0-100)";
      // 
      // nudMaxVolume
      // 
      nudMaxVolume.Location = new Point(141, 89);
      nudMaxVolume.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxVolume.Name = "nudMaxVolume";
      nudMaxVolume.Size = new Size(64, 23);
      nudMaxVolume.TabIndex = 7;
      nudMaxVolume.Value = new decimal(new int[] { 1, 0, 0, 0 });
      nudMaxVolume.ValueChanged += nudMaxVolume_ValueChanged;
      // 
      // btnImportPlaylist
      // 
      btnImportPlaylist.Location = new Point(218, 2);
      btnImportPlaylist.Name = "btnImportPlaylist";
      btnImportPlaylist.Size = new Size(107, 23);
      btnImportPlaylist.TabIndex = 8;
      btnImportPlaylist.Text = "Import Playlist";
      btnImportPlaylist.UseVisualStyleBackColor = true;
      btnImportPlaylist.Click += btnImportPlaylist_Click;
      // 
      // btnExportPlaylist
      // 
      btnExportPlaylist.Location = new Point(218, 31);
      btnExportPlaylist.Name = "btnExportPlaylist";
      btnExportPlaylist.Size = new Size(107, 23);
      btnExportPlaylist.TabIndex = 9;
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
      btnSkip.Location = new Point(227, 3);
      btnSkip.Name = "btnSkip";
      btnSkip.Size = new Size(49, 23);
      btnSkip.TabIndex = 2;
      btnSkip.Text = "Skip";
      btnSkip.UseVisualStyleBackColor = true;
      btnSkip.Click += btnSkip_Click;
      // 
      // lblCurrSong
      // 
      lblCurrSong.AutoSize = true;
      lblCurrSong.Location = new Point(5, 95);
      lblCurrSong.Name = "lblCurrSong";
      lblCurrSong.Size = new Size(37, 30);
      lblCurrSong.TabIndex = 22;
      lblCurrSong.Text = "Song:\r\n ";
      lblCurrSong.UseMnemonic = false;
      // 
      // btnPrev
      // 
      btnPrev.Location = new Point(272, 2);
      btnPrev.Name = "btnPrev";
      btnPrev.Size = new Size(53, 42);
      btnPrev.TabIndex = 15;
      btnPrev.Text = "Go Back";
      btnPrev.UseVisualStyleBackColor = true;
      btnPrev.Click += btnPrev_Click;
      // 
      // btnSaveCurrToPlaylist
      // 
      btnSaveCurrToPlaylist.Location = new Point(131, 2);
      btnSaveCurrToPlaylist.Name = "btnSaveCurrToPlaylist";
      btnSaveCurrToPlaylist.Size = new Size(58, 42);
      btnSaveCurrToPlaylist.TabIndex = 13;
      btnSaveCurrToPlaylist.Text = "Save Current";
      btnSaveCurrToPlaylist.UseVisualStyleBackColor = true;
      btnSaveCurrToPlaylist.Click += btnSaveCurrToPlaylist_Click;
      // 
      // btnSavePrevToPlaylist
      // 
      btnSavePrevToPlaylist.Location = new Point(195, 2);
      btnSavePrevToPlaylist.Name = "btnSavePrevToPlaylist";
      btnSavePrevToPlaylist.Size = new Size(71, 42);
      btnSavePrevToPlaylist.TabIndex = 14;
      btnSavePrevToPlaylist.Text = "Save Previous";
      btnSavePrevToPlaylist.UseVisualStyleBackColor = true;
      btnSavePrevToPlaylist.Click += btnSavePrevToPlaylist_Click;
      // 
      // btnTogglePlayPause
      // 
      btnTogglePlayPause.Location = new Point(282, 3);
      btnTogglePlayPause.Name = "btnTogglePlayPause";
      btnTogglePlayPause.Size = new Size(52, 23);
      btnTogglePlayPause.TabIndex = 3;
      btnTogglePlayPause.Text = "Pause";
      btnTogglePlayPause.UseVisualStyleBackColor = true;
      btnTogglePlayPause.Click += btnTogglePlayPause_Click;
      // 
      // lblQueueSize
      // 
      lblQueueSize.AutoSize = true;
      lblQueueSize.ForeColor = SystemColors.GrayText;
      lblQueueSize.Location = new Point(5, 80);
      lblQueueSize.Name = "lblQueueSize";
      lblQueueSize.Size = new Size(62, 15);
      lblQueueSize.TabIndex = 21;
      lblQueueSize.Text = "0 in queue";
      // 
      // lblPlaylist
      // 
      lblPlaylist.AutoSize = true;
      lblPlaylist.Location = new Point(3, 8);
      lblPlaylist.Name = "lblPlaylist";
      lblPlaylist.Size = new Size(47, 30);
      lblPlaylist.TabIndex = 11;
      lblPlaylist.Text = "Playlist:\r ";
      // 
      // btnRemoveCurrFromPlaylist
      // 
      btnRemoveCurrFromPlaylist.Location = new Point(56, 2);
      btnRemoveCurrFromPlaylist.Name = "btnRemoveCurrFromPlaylist";
      btnRemoveCurrFromPlaylist.Size = new Size(69, 42);
      btnRemoveCurrFromPlaylist.TabIndex = 12;
      btnRemoveCurrFromPlaylist.Text = "Remove Current";
      btnRemoveCurrFromPlaylist.UseVisualStyleBackColor = true;
      btnRemoveCurrFromPlaylist.Click += btnRemoveCurrFromPlaylist_Click;
      // 
      // dgvQueueAndPlaylist
      // 
      dgvQueueAndPlaylist.AllowUserToAddRows = false;
      dgvQueueAndPlaylist.AllowUserToDeleteRows = false;
      dgvQueueAndPlaylist.AllowUserToOrderColumns = true;
      dgvQueueAndPlaylist.AllowUserToResizeRows = false;
      dgvQueueAndPlaylist.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      dgvQueueAndPlaylist.BackgroundColor = SystemColors.ControlLightLight;
      dgvQueueAndPlaylist.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
      dgvQueueAndPlaylist.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      dgvQueueAndPlaylist.Columns.AddRange(new DataGridViewColumn[] { Index, Title, Author, Duration, RequestedBy, VideoId });
      dgvQueueAndPlaylist.Location = new Point(-1, 128);
      dgvQueueAndPlaylist.Name = "dgvQueueAndPlaylist";
      dgvQueueAndPlaylist.ReadOnly = true;
      dgvQueueAndPlaylist.RowHeadersVisible = false;
      dgvQueueAndPlaylist.RowTemplate.Height = 25;
      dgvQueueAndPlaylist.SelectionMode = DataGridViewSelectionMode.CellSelect;
      dgvQueueAndPlaylist.ShowCellToolTips = false;
      dgvQueueAndPlaylist.Size = new Size(589, 150);
      dgvQueueAndPlaylist.StandardTab = true;
      dgvQueueAndPlaylist.TabIndex = 23;
      dgvQueueAndPlaylist.CellContentDoubleClick += dgvQueueAndPlaylist_CellContentDoubleClick;
      dgvQueueAndPlaylist.Leave += dgvQueueAndPlaylist_Leave;
      // 
      // Index
      // 
      Index.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
      Index.HeaderText = "#";
      Index.Name = "Index";
      Index.ReadOnly = true;
      Index.Resizable = DataGridViewTriState.False;
      Index.SortMode = DataGridViewColumnSortMode.NotSortable;
      Index.Width = 20;
      // 
      // Title
      // 
      Title.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
      Title.HeaderText = "Title";
      Title.Name = "Title";
      Title.ReadOnly = true;
      Title.SortMode = DataGridViewColumnSortMode.NotSortable;
      // 
      // Author
      // 
      Author.HeaderText = "Author";
      Author.Name = "Author";
      Author.ReadOnly = true;
      Author.SortMode = DataGridViewColumnSortMode.NotSortable;
      // 
      // Duration
      // 
      Duration.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
      Duration.HeaderText = "Length";
      Duration.Name = "Duration";
      Duration.ReadOnly = true;
      Duration.Resizable = DataGridViewTriState.False;
      Duration.TrackVisitedState = false;
      Duration.Width = 50;
      // 
      // RequestedBy
      // 
      RequestedBy.HeaderText = "Requested By";
      RequestedBy.Name = "RequestedBy";
      RequestedBy.ReadOnly = true;
      RequestedBy.SortMode = DataGridViewColumnSortMode.NotSortable;
      // 
      // VideoId
      // 
      VideoId.HeaderText = "VideoId";
      VideoId.Name = "VideoId";
      VideoId.ReadOnly = true;
      VideoId.Resizable = DataGridViewTriState.False;
      VideoId.SortMode = DataGridViewColumnSortMode.NotSortable;
      VideoId.Visible = false;
      // 
      // panel1
      // 
      panel1.Controls.Add(lblPlaylist);
      panel1.Controls.Add(cbIsSearchRegex);
      panel1.Controls.Add(txtSearch);
      panel1.Controls.Add(lblCurrSong);
      panel1.Controls.Add(lblQueueSize);
      panel1.Controls.Add(btnRemoveCurrFromPlaylist);
      panel1.Controls.Add(dgvQueueAndPlaylist);
      panel1.Controls.Add(label6);
      panel1.Controls.Add(sliderVolume);
      panel1.Controls.Add(labelVolume);
      panel1.Controls.Add(btnPrev);
      panel1.Controls.Add(btnSavePrevToPlaylist);
      panel1.Controls.Add(btnSaveCurrToPlaylist);
      panel1.Dock = DockStyle.Fill;
      panel1.Location = new Point(0, 144);
      panel1.Margin = new Padding(0);
      panel1.Name = "panel1";
      panel1.Size = new Size(587, 277);
      panel1.TabIndex = 2;
      // 
      // cbIsSearchRegex
      // 
      cbIsSearchRegex.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      cbIsSearchRegex.AutoSize = true;
      cbIsSearchRegex.Location = new Point(426, 107);
      cbIsSearchRegex.Name = "cbIsSearchRegex";
      cbIsSearchRegex.Size = new Size(58, 19);
      cbIsSearchRegex.TabIndex = 25;
      cbIsSearchRegex.Text = "Regex";
      cbIsSearchRegex.UseVisualStyleBackColor = true;
      cbIsSearchRegex.CheckedChanged += cbIsSearchRegex_CheckedChanged;
      // 
      // txtSearch
      // 
      txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      txtSearch.Location = new Point(487, 105);
      txtSearch.Name = "txtSearch";
      txtSearch.Size = new Size(100, 23);
      txtSearch.TabIndex = 24;
      // 
      // panelSettings
      // 
      panelSettings.AutoSize = true;
      panelSettings.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      panelSettings.Controls.Add(label1);
      panelSettings.Controls.Add(nudMinSeconds);
      panelSettings.Controls.Add(label2);
      panelSettings.Controls.Add(nudMaxSeconds);
      panelSettings.Controls.Add(btnExportPlaylist);
      panelSettings.Controls.Add(label3);
      panelSettings.Controls.Add(btnImportPlaylist);
      panelSettings.Controls.Add(nudMaxPerUser);
      panelSettings.Controls.Add(nudMaxVolume);
      panelSettings.Controls.Add(label4);
      panelSettings.Dock = DockStyle.Top;
      panelSettings.Location = new Point(0, 0);
      panelSettings.Margin = new Padding(0);
      panelSettings.MinimumSize = new Size(328, 115);
      panelSettings.Name = "panelSettings";
      panelSettings.Size = new Size(587, 115);
      panelSettings.TabIndex = 0;
      panelSettings.Visible = false;
      // 
      // panel4
      // 
      panel4.AutoSize = true;
      panel4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      panel4.Controls.Add(btnShowHideSettings);
      panel4.Controls.Add(btnTogglePlayPause);
      panel4.Controls.Add(btnShowYoutubeForm);
      panel4.Controls.Add(btnSkip);
      panel4.Dock = DockStyle.Top;
      panel4.Location = new Point(0, 115);
      panel4.Margin = new Padding(0);
      panel4.MinimumSize = new Size(226, 29);
      panel4.Name = "panel4";
      panel4.Size = new Size(587, 29);
      panel4.TabIndex = 1;
      // 
      // btnShowHideSettings
      // 
      btnShowHideSettings.Location = new Point(3, 3);
      btnShowHideSettings.Name = "btnShowHideSettings";
      btnShowHideSettings.Size = new Size(105, 23);
      btnShowHideSettings.TabIndex = 0;
      btnShowHideSettings.Text = "Show Settings";
      btnShowHideSettings.UseVisualStyleBackColor = true;
      btnShowHideSettings.Click += btnShowHideSettings_Click;
      // 
      // SongRequestView
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(panel1);
      Controls.Add(panel4);
      Controls.Add(panelSettings);
      DoubleBuffered = true;
      MinimumSize = new Size(587, 225);
      Name = "SongRequestView";
      Size = new Size(587, 421);
      ((System.ComponentModel.ISupportInitialize)nudMinSeconds).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxSeconds).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxPerUser).EndInit();
      ((System.ComponentModel.ISupportInitialize)sliderVolume).EndInit();
      ((System.ComponentModel.ISupportInitialize)nudMaxVolume).EndInit();
      ((System.ComponentModel.ISupportInitialize)dgvQueueAndPlaylist).EndInit();
      panel1.ResumeLayout(false);
      panel1.PerformLayout();
      panelSettings.ResumeLayout(false);
      panelSettings.PerformLayout();
      panel4.ResumeLayout(false);
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
    private Button btnPrev;
    private Button btnSaveCurrToPlaylist;
    private Button btnSavePrevToPlaylist;
    private Button btnTogglePlayPause;
    private Label lblQueueSize;
    private Label lblPlaylist;
    private Button btnRemoveCurrFromPlaylist;
    private DataGridView dgvQueueAndPlaylist;
    private DataGridViewTextBoxColumn Index;
    private DataGridViewTextBoxColumn Title;
    private DataGridViewTextBoxColumn Author;
    private DataGridViewLinkColumn Duration;
    private DataGridViewTextBoxColumn RequestedBy;
    private DataGridViewTextBoxColumn VideoId;
    private Panel panel1;
    private Panel panelSettings;
    private Panel panel4;
    private Button btnShowHideSettings;
    private TextBox txtSearch;
    private CheckBox cbIsSearchRegex;
  }
}
