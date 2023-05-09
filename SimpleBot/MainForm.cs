namespace SimpleBot
{
  public partial class MainForm : Form
  {
    public static MainForm Get;
    readonly Bot bot = null;

    public MainForm()
    {
      Get = this;
      InitializeComponent();

#if DEBUG
      Text += " (debug)";
#endif

      bot = new Bot();
      bot.UpdatedTwitchConnected += Bot_UpdatedTwitchConnected;
      ChatActivity.UpdatedUsersInChat += ((EventHandler)Bot_UpdatedUsersInChat).Debounce(10000);
      _ = bot.Init().ThrowMainThread();
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
      var users = ChatActivity.UsersInChat();
      this.Invoke(() =>
      {
        listChatters.Items.Clear();
        listChatters.SuspendLayout();
        foreach (var user in users)
          listChatters.Items.Add(user);
        listChatters.ResumeLayout(true);
      });
    }
  }
}