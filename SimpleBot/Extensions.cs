﻿using Newtonsoft.Json;

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

    public static string ToJson(this object o) => JsonConvert.SerializeObject(o);
    public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);

    public static Task LogErr(this Task task) => task.ContinueWith(t =>
    {
      if (t.Exception != null)
        Bot.Log("[Task Err] " + t.Exception);
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

    public static Task RunThreadSTA(this Func<Task> action)
    {
      var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var thread = new Thread(() =>
      {
        Application.Idle += Application_Idle;
        Application.Run();
      });
      thread.SetApartmentState(ApartmentState.STA);
      thread.IsBackground = true;
      thread.Start();
      return tcs.Task;

      async void Application_Idle(object sender, EventArgs e)
      {
        Application.Idle -= Application_Idle;
        try
        {
          await action();
          tcs.SetResult();
        }
        catch (Exception ex)
        {
          tcs.SetException(ex);
          Application.ExitThread();
        }
      }
    }
  }
}
