using Lucene.Net.Search;
using System.Diagnostics;
using System.Text;
using static Lucene.Net.Util.Fst.Util;
using System.Web;
using System.Net;
using static SimpleBot.Youtube;

namespace SimpleBot
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            /*
            var bot = new BotV2();
            Application.Run();

            //CommandCompiler._test_parseString();
            var h = CommandCompiler.CreateHandler("${ meep } Hi $query $m $$ ", 0);
            h(new() { name = "I'm a real person" }, "boop", ["a", "b", "c"], "a b c");
            return;
            */

            // Refetch data in playlist (SongRequest) -- remember that changes won't save in Debug mode
            if (false)
            {
                Task.Run(async () =>
                {
                    Bot bot = new(null); // load data
                    await SongRequest.Init(bot, null);
                    await Task.Delay(2000);
                    _ = SongRequest._yt.PauseOrResume();

                    MessageBox.Show("It has begun");

                    var sb = new StringBuilder();
                    var sb_failedUpdates = new StringBuilder();
                    int currUpdate = 0;
                    var res = await SongRequest.RefetchDataInPlaylist(
              searchByTitleIfNoResultById: false,
              isDirtyPredicate: r => true,// r => r.author == null || r.author.EndsWith(" - topic", StringComparison.InvariantCultureIgnoreCase),
              onUpdate_beforeAndAfter: (before, after) =>
              {
                        if ((++currUpdate % 10) == 0)
                            Debug.WriteLine(currUpdate + " updates");

                        if (after.ytVideoId == null)
                            sb_failedUpdates.AppendLine(before.ToLongString());
                        else
                        {
                            if (before.ytVideoId != after.ytVideoId)
                                sb.AppendLine("[SUS]");
                            sb.Append("    ").AppendLine(before.ToLongString())
                      .Append(" -> ").AppendLine(after.ToLongString())
                      .AppendLine();
                        }
                    }
            );
                    sb.AppendLine();

                    if (sb_failedUpdates.Length > 0)
                        sb.AppendLine("Refetches that had no results:").Append(sb_failedUpdates.ToString());

                    sb.Append(res.updatedCount).Append(" updated, ")
              .Append(res.dirtyCount - res.updatedCount).Append(" unchanged.")
              .AppendLine();

                    string output = sb.ToString();
                    Debug.WriteLine(output);

                    MessageBox.Show("Done");
                    Clipboard.SetText(output);
                }).GetAwaiter().GetResult();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (o, e) =>
            {
                MainForm.UnregisterHotKeys();
                ChatterDataMgr._save_noLock();
                Bot.Log("[final words] thread exception: " + e.Exception);
                Environment.FailFast(null, e.Exception);
            };
            Application.ApplicationExit += (o, e) =>
            {
                MainForm.UnregisterHotKeys();
                ChatterDataMgr._save_noLock();
                Bot.Log("[final words] application exit");
            };

            Application.Run(new MainForm());
        }
    }
}