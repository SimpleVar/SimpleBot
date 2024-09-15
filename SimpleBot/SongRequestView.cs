using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SimpleBot
{
    public partial class SongRequestView : UserControl
    {
        const string PAUSE_TEXT = "❚❚";
        const string PLAY_TEXT = "▶";

        public SongRequestView()
        {
            InitializeComponent();
            btnTogglePlayPause.Text = PAUSE_TEXT;

            SongRequest.NeedUpdateUI_Volume += SongRequest_NeedUpdateUI_Volume;
            SongRequest.NeedUpdateUI_SongList += SongRequest_NeedUpdateUI_SongList;
            SongRequest.NeedUpdateUI_Paused += (o, paused) => BeginInvoke(() => btnTogglePlayPause.Text = paused ? PLAY_TEXT : PAUSE_TEXT);

            nudMinSeconds.Value = SongRequest.SR_minDuration_inSeconds;
            nudMaxSeconds.Value = SongRequest.SR_maxDuration_inSeconds;
            nudMaxPerUser.Value = SongRequest.SR_maxSongsInQueuePerUser;
            UpdateVolumeDisplay(SongRequest._GetVolume());
            nudMaxVolume.Value = sliderVolume.Maximum;

            txtSearch.TextChanged += ((EventHandler)TxtSearch_TextChanged).Debounce(100);
        }

        private void SongRequestView_Load(object sender, EventArgs e)
        {
            // title is initially Fill, and author is NotSet - this makes the title take nice space
            // but then we swap their AutoSizeMode for better UX when the manually resizing column
            dgvQueueAndPlaylist.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            dgvQueueAndPlaylist.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void cbIsSearchRegex_CheckedChanged(object sender, EventArgs e)
        {
            filterRows();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // BeginInvoke - this event is debounced, therefore we're not running on main thread
            BeginInvoke(filterRows);
        }

        void filterRows()
        {
            Regex rgx = null;
            txtSearch.ForeColor = SystemColors.WindowText;
            if (cbIsSearchRegex.Checked)
            {
                try
                {
                    rgx = new Regex(txtSearch.Text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
                }
                catch
                {
                    txtSearch.ForeColor = Color.IndianRed;
                }
            }

            dgvQueueAndPlaylist.SuspendLayout();
            var autoSizeModes = new DataGridViewAutoSizeColumnMode[dgvQueueAndPlaylist.ColumnCount];
            for (int i = 0; i < dgvQueueAndPlaylist.ColumnCount; i++)
            {
                var col = dgvQueueAndPlaylist.Columns[i];
                autoSizeModes[i] = col.AutoSizeMode;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            if (rgx != null)
            {
                for (int i = 0; i < dgvQueueAndPlaylist.RowCount; i++)
                {
                    var row = dgvQueueAndPlaylist.Rows[i];
                    row.Visible = rgx.IsMatch((string)row.Cells[1].Value) || (!string.IsNullOrEmpty((string)row.Cells[4].Value) && rgx.IsMatch((string)row.Cells[4].Value));
                }
            }
            else
            {
                for (int i = 0; i < dgvQueueAndPlaylist.RowCount; i++)
                {
                    var row = dgvQueueAndPlaylist.Rows[i];
                    row.Visible = ((string)row.Cells[1].Value).Contains(txtSearch.Text, StringComparison.InvariantCultureIgnoreCase)
                               || (!string.IsNullOrEmpty((string)row.Cells[4].Value) && ((string)row.Cells[4].Value).Contains(txtSearch.Text, StringComparison.InvariantCultureIgnoreCase));
                }
            }

            for (int i = 0; i < dgvQueueAndPlaylist.ColumnCount; i++)
                dgvQueueAndPlaylist.Columns[i].AutoSizeMode = autoSizeModes[i];

            dgvQueueAndPlaylist.ClearSelection();
            dgvQueueAndPlaylist.ResumeLayout(true);
        }

        private void dgvQueueAndPlaylist_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 3)
                return;
            var videoId = dgvQueueAndPlaylist.Rows[e.RowIndex].Cells[5].Value as string;
            if (string.IsNullOrEmpty(videoId))
                return;

            Process.Start(new ProcessStartInfo("https://youtu.be/" + videoId) { UseShellExecute = true });
        }

        private void SongRequest_NeedUpdateUI_SongList(object sender, SongRequest.SRData e)
        {
            // event comes from different thread
            BeginInvoke(() =>
            {
                bool isCurrFromPlaylist = e.CurrSong.ytVideoId == e.Playlist[e.CurrIndexToPlayInPlaylist].ytVideoId;
                lblCurrSong.Text = $"{e.CurrSong.ToLongString(includeLink: false)}\r\n{(isCurrFromPlaylist ? "" : "*")}Requested by: {e.CurrSong.ogRequesterDisplayName}";
                lblQueueSize.Text = e.Queue.Count + " in queue";
                lblPlaylistLength.Text = "size: " + e.Playlist.Count;

                dgvQueueAndPlaylist.SuspendLayout();
                dgvQueueAndPlaylist.Rows.Clear();
                // queue
                for (int i = 0; i < e.Queue.Count; i++)
                {
                    var q = e.Queue[i];
                    dgvQueueAndPlaylist.Rows.Add(i + 1, q.title, q.author, q.duration, q.ogRequesterDisplayName, q.ytVideoId);
                }
                // playlist
                for (int i = e.CurrIndexToPlayInPlaylist + 1; i < e.Playlist.Count; i++)
                {
                    var p = e.Playlist[i];
                    dgvQueueAndPlaylist.Rows.Add("", p.title, p.author, p.duration, p.ogRequesterDisplayName, p.ytVideoId);
                }
                for (int i = 0; i <= e.CurrIndexToPlayInPlaylist; i++)
                {
                    var p = e.Playlist[i];
                    dgvQueueAndPlaylist.Rows.Add("", p.title, p.author, p.duration, p.ogRequesterDisplayName, p.ytVideoId);
                }
                dgvQueueAndPlaylist.ClearSelection();
                dgvQueueAndPlaylist.ResumeLayout(true);
            });
        }

        private void SongRequest_NeedUpdateUI_Volume(object sender, (int volume, int maxVolume) e)
        {
            // event comes from different thread
            BeginInvoke(() => UpdateVolumeDisplay(e));
        }

        void UpdateVolumeDisplay((int volume, int maxVolume) e)
        {
            // don't update nudMaxVolume.Value, the UI itself is the original cause of the maxVolume value change
            sliderVolume.Maximum = e.maxVolume;
            sliderVolume.Value = e.volume;
            labelVolume.Text = e.volume + "";
        }

        private void btnShowYoutubeForm_Click(object sender, EventArgs e)
        {
            SongRequest._yt?.ShowOrHide(this, isVisible =>
            {
                btnShowYoutubeForm.ForeColor = isVisible ? Color.ForestGreen : SystemColors.ControlText;
            });
        }

        private void nudMinSeconds_ValueChanged(object sender, EventArgs e)
        {
            SongRequest.SR_minDuration_inSeconds = (int)nudMinSeconds.Value;
        }

        private void nudMaxSeconds_ValueChanged(object sender, EventArgs e)
        {
            SongRequest.SR_maxDuration_inSeconds = (int)nudMaxSeconds.Value;
        }

        private void nudMaxPerUser_ValueChanged(object sender, EventArgs e)
        {
            SongRequest.SR_maxSongsInQueuePerUser = (int)nudMaxPerUser.Value;
        }

        private void nudMaxVolume_ValueChanged(object sender, EventArgs e)
        {
            Task.Run(async () => await SongRequest._SetMaxVolume((int)nudMaxVolume.Value)).LogErr();
        }

        private void sliderVolume_Scroll(object sender, EventArgs e)
        {
            int vol = sliderVolume.Value;
            labelVolume.Text = vol + "";
            Task.Run(async () => await SongRequest._SetVolume(vol)).LogErr();
        }

        private void btnImportPlaylist_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            SongRequest.Req[] songs;
            try
            {
                songs = File.ReadAllText(ofd.FileName).FromJson<SongRequest.Req[]>();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return;
            }
            SongRequest.ImportToPlaylist_nochecks(songs);
        }

        private void btnExportPlaylist_Click(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            var json = SongRequest.GetPlaylist().ToJson();
            File.WriteAllText(sfd.FileName, json);
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            SongRequest.Next();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            SongRequest.PlaylistBackOne();
        }

        private void btnSaveCurrToPlaylist_Click(object sender, EventArgs e)
        {
            SongRequest.SaveCurrSongToPlaylist();
        }

        private void btnSavePrevToPlaylist_Click(object sender, EventArgs e)
        {
            SongRequest.SavePrevSongToPlaylist();
        }

        private void btnRemoveCurrFromPlaylist_Click(object sender, EventArgs e)
        {
            SongRequest.RemoveCurrSongFromPlaylist();
        }

        private async void btnTogglePlayPause_Click(object sender, EventArgs e)
        {
            await SongRequest.PlayPause();
        }

        private void btnShowHideSettings_Click(object sender, EventArgs e)
        {
            btnShowHideSettings.Text = (panelSettings.Visible = !panelSettings.Visible) ? "Hide Settings" : "Show Settings";
        }

        private void dgvQueueAndPlaylist_Leave(object sender, EventArgs e)
        {
            dgvQueueAndPlaylist.ClearSelection();
        }

        private void ctxMenuItem_moveToTop_Click(object sender, EventArgs e)
        {
            var ids = GetSelectedVideoIds();
            SongRequest.MoveToTop(ids);
        }

        private void ctxMenuItem_delete_Click(object sender, EventArgs e)
        {
            var ids = GetSelectedVideoIds();
            SongRequest.RemoveManySongsFromPlaylist(ids);
        }

        private void dgvQueueAndPlaylist_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var res = dgvQueueAndPlaylist.HitTest(e.X, e.Y);
            switch (res.Type)
            {
                case DataGridViewHitTestType.Cell:
                    var c = dgvQueueAndPlaylist.Rows[res.RowIndex].Cells[res.ColumnIndex];
                    if (!c.Selected)
                    {
                        dgvQueueAndPlaylist.ClearSelection();
                        c.Selected = true;
                    }
                    break;
                case DataGridViewHitTestType.RowHeader:
                    dgvQueueAndPlaylist.ClearSelection();
                    dgvQueueAndPlaylist.Rows[res.RowIndex].Cells[1].Selected = true;
                    break;
                default:
                    return;
            }
            showDgvContextMenu(dgvQueueAndPlaylist.PointToScreen(e.Location));
        }

        private void dgvQueueAndPlaylist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Apps || dgvQueueAndPlaylist.SelectedCells.Count == 0)
                return;

            var dgv = dgvQueueAndPlaylist;
            showDgvContextMenu(dgv.PointToScreen(new Point(dgv.GetRowDisplayRectangle(dgv.FirstDisplayedCell.RowIndex, true).Right - 2, 2)), ToolStripDropDownDirection.Left);
        }

        void showDgvContextMenu(Point pos, ToolStripDropDownDirection dir)
        {
            beforeShowing_DgvContextMenu();
            ctx.Show(pos, dir);
        }

        void showDgvContextMenu(Point pos)
        {
            beforeShowing_DgvContextMenu();
            ctx.Show(pos);
        }

        void beforeShowing_DgvContextMenu()
        {
            var selectedRows = 0;
            GoOverSelectedRows(_ => selectedRows++);
            if (selectedRows == 0)
                return;
            ctxMenuItem_lblSelectedAmount.Text = selectedRows == 1 ? "(1 song selected)" : "(" + selectedRows + " songs selected)";
        }

        static int _lastTag = 0;
        void GoOverSelectedRows(Action<int> cb)
        {
            var cells = dgvQueueAndPlaylist.SelectedCells;
            object tag = ++_lastTag;
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var rowIdx = c.RowIndex;
                var row = dgvQueueAndPlaylist.Rows[rowIdx];
                if (row.Tag == tag)
                    continue;
                row.Tag = tag;
                cb(rowIdx);
            }
        }

        HashSet<string> GetSelectedVideoIds()
        {
            HashSet<string> ids = new();
            GoOverSelectedRows(i =>
            {
                var videoId = dgvQueueAndPlaylist.Rows[i].Cells[5].Value as string;
                ids.Add(videoId);
            });
            return ids;
        }
    }
}
