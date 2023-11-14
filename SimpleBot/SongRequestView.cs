using System.Diagnostics;

namespace SimpleBot
{
  public partial class SongRequestView : UserControl
  {
    public SongRequestView()
    {
      InitializeComponent();

      SongRequest.NeedUpdateUI_Volume += SongRequest_NeedUpdateUI_Volume;
      SongRequest.NeedUpdateUI_SongList += SongRequest_NeedUpdateUI_SongList;

      nudMinSeconds.Value = SongRequest.SR_minDuration_inSeconds;
      nudMaxSeconds.Value = SongRequest.SR_maxDuration_inSeconds;
      nudMaxPerUser.Value = SongRequest.SR_maxSongsInQueuePerUser;
      UpdateVolumeDisplay(SongRequest._GetVolume());
      nudMaxVolume.Value = sliderVolume.Maximum;
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
      BeginInvoke(() =>
      {
        lblCurrSong.Text = $"{e.CurrSong.ToLongString(includeLink: false)}\r\nRequested by: {e.CurrSong.ogRequesterDisplayName}";
        lblQueueSize.Text = e.Queue.Count + " in queue";

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
        dgvQueueAndPlaylist.ResumeLayout(true);
      });
    }

    private void SongRequest_NeedUpdateUI_Volume(object sender, (int volume, int maxVolume) e)
    {
      Invoke(() => UpdateVolumeDisplay(e));
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
      var playing = await SongRequest._yt.PauseOrResume() == "1";
      btnTogglePlayPause.Text = playing ? "Pause" : "Play";
    }

    private void btnShowHideSettings_Click(object sender, EventArgs e)
    {
      btnShowHideSettings.Text = (panelSettings.Visible = !panelSettings.Visible) ? "Hide Settings" : "Show Settings";
    }
  }
}
