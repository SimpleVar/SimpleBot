using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace SimpleBot
{
  public partial class MainForm : Form
  {
    public static MainForm Get;
    readonly Bot bot = null;
    bool _freezeChattersList = false;
    bool _isMultiselectingChatters = false;

    public MainForm()
    {
      Get = this;
      InitializeComponent();

#if DEBUG
      Text += " (debug)";
#endif

      bot = new Bot();
      bot.UpdatedTwitchConnected += Bot_UpdatedTwitchConnected;
      bot.BadCredentials += Bot_BadCredentials;
      ChatActivity.UpdatedUsersInChat += ((EventHandler)Bot_UpdatedUsersInChat).Debounce(10000);
      _ = bot.Init().ThrowMainThread();
    }

    private void Bot_BadCredentials(object sender, EventArgs e)
    {
      MessageBox.Show("Failed to authenticate or something like that, closing now...");
      Application.Exit();
    }

    private async void Bot_UpdatedTwitchConnected(object sender, EventArgs e)
    {
      await Task.Delay(100);
      var isConnected = bot._tw.IsConnected;
      this.Invoke(() =>
      {
        if (isConnected)
        {
          lblTwConnected.Text = "Twitch connected (" + bot.CHANNEL + ")";
          lblTwConnected.ForeColor = Color.ForestGreen;
        }
        else
        {
          lblTwConnected.Text = "Twitch diconnected";
          lblTwConnected.ForeColor = Color.Firebrick;
        }
      });
    }

    private void Bot_UpdatedUsersInChat(object sender, EventArgs e)
    {
      if (_freezeChattersList)
        return;
      var users = ChatActivity.UsersInChat();
      Array.Sort(users);
      this.Invoke(() =>
      {
        labelChatters.Text = $"Chatters: ({users.Length})";
        listChatters.Items.Clear();
        listChatters.SuspendLayout();
        foreach (var user in users)
          listChatters.Items.Add(user);
        listChatters.ResumeLayout(true);
      });
    }

    async Task<bool> Ban(string name, string reason)
    {
      var success = false;
      try
      {
        var uid = await bot._twApi.GetUserId(name.CanonicalUsername()).ConfigureAwait(true);
        if (uid == null)
        {
          Debug.WriteLine("[Mass ban] " + name + " does not exist");
          return true;
        }
        if ((await bot._twApi.Helix.Moderation.GetBannedUsersAsync(bot.CHANNEL_ID, userIds: new() { uid })).Data.Length > 0)
        {
          Debug.WriteLine("[Mass ban] " + name + " already banned");
          return true;
        }
        var res = await bot._twApi.Helix.Moderation.BanUserAsync(bot.CHANNEL_ID, bot.CHANNEL_ID, new()
        {
          UserId = uid,
          Reason = reason
        }).ConfigureAwait(true);
        success = res.Data.Length > 0;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
      }
      Debug.WriteLine($"[Mass ban] {(success ? "Banned" : "Failed to ban")} {name}");
      return success;
    }

    // https://twitchinsights.net/bots
    private async void btnMassBan_Click(object sender, EventArgs e)
    {
      ofd.Title = "Choose a list of twitch names to ban, separated by space/comma/tabs/lines";
      if (ofd.ShowDialog() != DialogResult.OK)
        return;

      var ogText = btnMassBan.Text;
      btnMassBan.Enabled = false;
      btnMassBan.Text = "banning...";
      int totBans = 0;
      int totFails = 0;
      var fileName = ofd.FileName;
      var listName = Path.GetFileNameWithoutExtension(fileName) ?? fileName;
      var reason = "mass ban list: " + listName;
      try
      {
        foreach (var line in File.ReadLines(fileName))
        {
          int i = 0;
          for (int j = 0; j < line.Length; j++)
          {
            if (!(line[j] is ' ' or ',' or '\n' or '\r' or '\t'))
              continue;
            if (i != j)
            {
              if (await Ban(line[i..j], reason).ConfigureAwait(true))
                totBans++;
              else
                totFails++;
              btnMassBan.Text = $"banning...\r\nbanned {totBans}, failed {totFails}";
            }
            i = j + 1;
          }
          if (i != line.Length)
          {
            if (await Ban(line[i..], reason).ConfigureAwait(true))
              totBans++;
            else
              totFails++;
            btnMassBan.Text = $"banning...\r\nbanned {totBans}, failed {totFails}";
          }
        }
      }
      catch (Exception ex)
      {
        Bot.Log($"[Mass ban: {listName}] ERROR: {ex}");
        btnMassBan.Text = "error, see logs";
        await Task.Delay(2000).ConfigureAwait(true);
      }
      finally
      {
        btnMassBan.Text = $"DONE\r\nbanned {totBans}, failed {totFails}";
        Bot.Log($"[Mass ban: {listName}] {totBans} bans and {totFails} fails");
        await Task.Delay(2000).ConfigureAwait(true);
        btnMassBan.Enabled = true;
        btnMassBan.Text = ogText;
      }
    }

    private void listChatters_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control && e.KeyCode == Keys.C)
      {
        Clipboard.SetText(string.Join("\r\n", listChatters.SelectedItems.Cast<object>()));
      }
    }

    private void cbFreezeChattersList_CheckedChanged(object sender, EventArgs e)
    {
      btnUpdateChatters.Enabled = _freezeChattersList;
      _freezeChattersList = !_freezeChattersList;
    }

    private void listChatters_SelectedIndexChanged(object sender, EventArgs e)
    {
      var multi = listChatters.SelectedIndices.Count > 1;
      if (_isMultiselectingChatters == multi)
        return;
      _isMultiselectingChatters = multi;
      // automatically freeze when multiselecting and unfreeze when dropping multiselection
      cbFreezeChattersList.Checked = multi;
    }

    private async void btnBanKnownBotsInChat_Click(object sender, EventArgs e)
    {
      try
      {
        btnBanKnownBotsInChat.Enabled = false;
        var users = ChatActivity.UsersInChat().ToHashSet();
        using var www = new HttpClient();
        var knownBots = JsonConvert.DeserializeObject<KnownBots>(await www.GetStringAsync("https://api.twitchinsights.net/v1/bots/all"));

        foreach (var knownBot in knownBots.bots)
        {
          long stalkCount = (long)knownBot[1];
          string name = (knownBot[0] + "").CanonicalUsername();
          if (
            users.Contains(name) &&
            stalkCount >= 30 &&
            name != bot.BOT_NAME.CanonicalUsername() &&
            name != "commanderroot" &&
            !ChatActivity.IsIgnoredBot(name)
            )
          {
            await Ban(name, $"known bot (stalkCount {stalkCount})");
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
      finally
      {
        btnBanKnownBotsInChat.Enabled = true;
      }
    }

    struct KnownBots
    {
      public object[][] bots;
    }
  }
}