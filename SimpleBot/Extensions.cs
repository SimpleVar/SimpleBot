using Newtonsoft.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleBot
{
  static class Extensions
  {
    public static string ReduceWhitespace(this string s)
    {
      StringBuilder res = null;
      bool isPrevWhiteSpace = false;
      for (int i = 0; i < s.Length; i++)
      {
        char c = s[i];
        bool ws = char.IsWhiteSpace(c);
        if (ws & isPrevWhiteSpace)
          continue;
        isPrevWhiteSpace = ws;
        res ??= new StringBuilder(s.Length);
        res.Append(c);
      }
      return res.ToString();
    }

    public static string ToShortDurationString(this TimeSpan dur)
    {
      dur = dur.Duration();
      if (dur.Days > 0)
        return dur.ToString(@"d\:h\:mm\:ss", CultureInfo.InvariantCulture);
      else if (dur.Hours > 0)
        return dur.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
      else
        return dur.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    public static T AtRand<T>(this T[] arr) => arr.Length == 0 ? default : arr[Rand.R.Next(arr.Length)];

    public static EventHandler Debounce(this EventHandler func, int ms)
    {
      var last = 0;
      return (o, arg) =>
      {
        var current = Interlocked.Increment(ref last);
        Task.Delay(ms).ContinueWith(task =>
        {
          if (current == last) func(o, arg);
          task.Dispose();
        });
      };
    }

    public static EventHandler<T> Debounce<T>(this EventHandler<T> func, int ms)
    {
      var last = 0;
      return (o, arg) =>
      {
        var current = Interlocked.Increment(ref last);
        Task.Delay(ms).ContinueWith(task =>
        {
          if (current == last) func(o, arg);
          task.Dispose();
        });
      };
    }

    public static string ToJson(this object o) => JsonConvert.SerializeObject(o);
    public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);

    public static Task LogErr(this Task task,
      [CallerMemberName] string memberName = "",
      [CallerFilePath] string sourceFilePath = "",
      [CallerLineNumber] int sourceLineNumber = 0) => task.ContinueWith(t =>
    {
      if (t.Exception != null)
      {
        Bot.Log($"[Task Err] Error from {memberName} at \"{sourceFilePath}:{sourceLineNumber}\": " + t.Exception);
        throw t.Exception;
      }
    });

    public static Task<T> LogErr<T>(this Task<T> task,
      [CallerMemberName] string memberName = "",
      [CallerFilePath] string sourceFilePath = "",
      [CallerLineNumber] int sourceLineNumber = 0) => task.ContinueWith(t =>
    {
      if (t.Exception != null)
      {
        Bot.Log($"[Task Err] Error from {memberName} at \"{sourceFilePath}:{sourceLineNumber}\": " + t.Exception);
        throw t.Exception;
      }
      return t.Result;
    });

    public static Task ThrowMainThread(this Task task) => task.ContinueWith(t =>
    {
      if (t.Exception != null) MainForm.Get.Invoke(() => {
        var ex = t.Exception?.InnerException ?? t.Exception;
        Application.OnThreadException(ex);
      });
    });

    public static Task<T> ThrowMainThread<T>(this Task<T> task) => task.ContinueWith(t =>
    {
      if (t.Exception != null) MainForm.Get.Invoke(() =>
      {
        var ex = t.Exception?.InnerException ?? t.Exception;
        Application.OnThreadException(ex);
      });
      return t.Result;
    });
  }
}
