using Newtonsoft.Json;

namespace SimpleBot
{
  static class Extensions
  {
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

    public static string NullIfEmpty(this string s) => string.IsNullOrEmpty(s) ? null : s;
    public static string NullIfWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    public static string ToJson(this object o) => JsonConvert.SerializeObject(o);
    public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);

    public static Task NoThrow(this Task task) => task.ContinueWith(t => {});
    public static Task<T> NoThrow<T>(this Task<T> task, T defaultVal = default) => task.ContinueWith(t => defaultVal);

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
