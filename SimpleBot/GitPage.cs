using Octokit;
using Octokit.Internal;
using System.Net;

namespace SimpleBot
{
  class GitPageSongList
  {
    public string stamp;
    public GitPageSong[] songs;
  }

  class GitPageSong
  {
    public string url;
    public string title;
    public int duration;
    public string req;
  }

  static class GitPage
  {
    private const string REPO_NAME = "SimpleVar.github.io";
    private const string OWNER_NAME = "SimpleVar";
    private const string SONGLIST_FILE = "songlist.json";

    private static readonly GitHubClient Client;
    private static string _lastSha;

    static GitPage()
    {
      Client = new GitHubClient(new ProductHeaderValue(REPO_NAME),
                                new InMemoryCredentialStore(new Credentials(Settings.Default.GitToken)));
    }

    public static async void UpdateSongList(GitPageSongList songlist)
    {
      if (_lastSha == null)
      {
        var x = Client.Repository.Content.GetAllContents(OWNER_NAME, REPO_NAME, SONGLIST_FILE);
        await x;
        _lastSha = x.Result[0].Sha;
      }
      
      var y = Client.Repository.Content.UpdateFile("SimpleVar",
                                                   "SimpleVar.github.io",
                                                   "songlist.json",
                                                   new UpdateFileRequest(".", songlist.ToJson(), _lastSha));
      await y;
      _lastSha = y.Result.Content.Sha;
    }

    public static async Task<GitPageSongList> GetSongList()
    {
      var x = Client.Repository.Content.GetAllContents(OWNER_NAME, REPO_NAME, SONGLIST_FILE);
      await x;
      var json = x.Result[0].Content;
      return json.FromJson<GitPageSongList>();
    }
  }
}
