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

    public static string ToJson(this object o)
    {
      return JsonConvert.SerializeObject(o);
    }

    public static T FromJson<T>(this string json) where T : class
    {
      try
      {
        return json == null ? null : JsonConvert.DeserializeObject<T>(json);
      }
      catch
      {
        return null;
      }
    }

    public static Task ThrowMainThread(this Task task) => task.ContinueWith(t =>
    {
      if (t.Exception != null) MainForm.Get.Invoke(() => throw t.Exception?.InnerException ?? t.Exception);
    });

    public static Task<T> ThrowMainThread<T>(this Task<T> task) => task.ContinueWith(t =>
    {
      if (t.Exception != null) MainForm.Get.Invoke(() => throw t.Exception?.InnerException ?? t.Exception);
      return t.Result;
    });
  }
}
