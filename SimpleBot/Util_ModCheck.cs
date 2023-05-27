using System.Diagnostics;
using System.Web;
using TwitchLib.Api;

namespace SimpleBot
{
  public partial class Util_ModCheck : Form
  {
    string ogTitle;

    public Util_ModCheck()
    {
      InitializeComponent();
      ogTitle = Text;
    }

    private void btnGenAuth_Click(object sender, EventArgs e)
    {
      Process proc = new Process();
      proc.StartInfo.UseShellExecute = true;
      proc.StartInfo.FileName = "https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=a4lltinre0n377a7l8mb4pd3g2odym&redirect_uri=http://localhost&scope=analytics%3Aread%3Aextensions%20analytics%3Aread%3Agames%20bits%3Aread%20channel%3Aedit%3Acommercial%20channel%3Amanage%3Abroadcast%20channel%3Aread%3Acharity%20channel%3Amanage%3Aextensions%20channel%3Amanage%3Amoderators%20channel%3Amanage%3Apolls%20channel%3Amanage%3Apredictions%20channel%3Amanage%3Araids%20channel%3Amanage%3Aredemptions%20channel%3Amanage%3Aschedule%20channel%3Amanage%3Avideos%20channel%3Aread%3Aeditors%20channel%3Aread%3Agoals%20channel%3Aread%3Ahype_train%20channel%3Aread%3Apolls%20channel%3Aread%3Apredictions%20channel%3Aread%3Aredemptions%20channel%3Aread%3Astream_key%20channel%3Aread%3Asubscriptions%20channel%3Aread%3Avips%20channel%3Amanage%3Avips%20clips%3Aedit%20moderation%3Aread%20moderator%3Amanage%3Aannouncements%20moderator%3Amanage%3Aautomod%20moderator%3Aread%3Aautomod_settings%20moderator%3Amanage%3Aautomod_settings%20moderator%3Amanage%3Abanned_users%20moderator%3Aread%3Ablocked_terms%20moderator%3Amanage%3Ablocked_terms%20moderator%3Amanage%3Achat_messages%20moderator%3Aread%3Achat_settings%20moderator%3Amanage%3Achat_settings%20moderator%3Aread%3Achatters%20moderator%3Aread%3Afollowers%20moderator%3Aread%3Ashield_mode%20moderator%3Amanage%3Ashield_mode%20moderator%3Aread%3Ashoutouts%20moderator%3Amanage%3Ashoutouts%20user%3Aedit%20user%3Aedit%3Afollows%20user%3Amanage%3Ablocked_users%20user%3Aread%3Ablocked_users%20user%3Aread%3Abroadcast%20user%3Amanage%3Achat_color%20user%3Aread%3Aemail%20user%3Aread%3Afollows%20user%3Aread%3Asubscriptions%20user%3Amanage%3Awhispers%20channel%3Amoderate%20chat%3Aedit%20chat%3Aread%20whispers%3Aread%20whispers%3Aedit";
      proc.Start();
    }

    private async void btnGo_Click(object sender, EventArgs e)
    {
      var ch = txtName.Text;
      var authUrl = txtAuthURL.Text;

      if (string.IsNullOrWhiteSpace(authUrl) || string.IsNullOrWhiteSpace(ch))
      {
        MessageBox.Show("Fill in the stuffs");
        return;
      }

      string accessToken = null;
      try
      {
        accessToken = HttpUtility.ParseQueryString(new Uri(authUrl).Fragment)["#access_token"];
      }
      catch
      {
        MessageBox.Show("Couldn't find read access_token from the auth redirected url");
        return;
      }

      btnGo.Invoke(() => btnGo.Enabled = true);
      Text = ogTitle;

      try
      {
        var api = new TwitchAPI();
        api.Settings.AccessToken = accessToken;
        api.Settings.ClientId = "a4lltinre0n377a7l8mb4pd3g2odym";
        string CH_ID = (await api.Helix.Search.SearchChannelsAsync(ch).ConfigureAwait(false)).Channels
          .FirstOrDefault(x => x.DisplayName.ToLowerInvariant() == ch.ToLowerInvariant())?.Id;

        var dd = await new TwitchApi_MoreEdges(api.Settings).GetAllFollows(CH_ID, accessToken).ConfigureAwait(false);
        var allFollows = string.Join(", ", dd.Select(x => x.broadcaster_name));
        txtAllFollows.Invoke(() => txtAllFollows.Text = allFollows);
        
        var mod_at = new List<string>();
        for (int i = 0; i < dd.Count; i++)
        {
          var u = dd[i];
          label6.Invoke(() => label6.Text = $"Channels in which you are mod: (scanning {i + 1}/{dd.Count})");
          bool mod = false;
          try
          {
            var x = await api.Helix.Chat.GetChattersAsync(u.broadcaster_id, CH_ID);
            mod = true;
          }
          catch { }
          if (mod) mod_at.Add(u.broadcaster_name);
        }

        label6.Invoke(() => label6.Text = "Channels in which you are mod:");
        var mmmm = string.Join(", ", mod_at);
        txtModChs.Invoke(() => txtModChs.Text = mmmm);
      }
      catch (Exception ex)
      {
        Text = ogTitle + " (ERROR)";
        MessageBox.Show(ex.ToString(), "Error");
        return;
      }
      finally
      {
        btnGo.Invoke(() => btnGo.Enabled = true);
      }
    }
  }
}