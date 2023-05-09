namespace SimpleBot
{
  internal class LongRunningPeriodicTask
  {
    readonly Thread _thread;
    long _lastPeriodId;

    public int DelayMsAfterWork;
    public int DelayMsInitial;
    public int DelayMsAfterEnabling;

    public volatile bool Enabled;

    static void sleep(int delayMs)
    {
      if (delayMs > 0)
        Thread.Sleep(delayMs);
    }

    public static LongRunningPeriodicTask Start(long lastPeriodId, bool enabled, int msAfterWork, int msInitial, int msAfterResume, Func<long, Task<int?>> work)
      => new(lastPeriodId, enabled, msAfterWork, msInitial, msAfterResume, work);
    public static LongRunningPeriodicTask Start(long lastPeriodId, bool enabled, int msAfterWork, int msInitial, int msAfterResume, Func<long, Task> work)
      => new(lastPeriodId, enabled, msAfterWork, msInitial, msAfterResume, async periodId => { await work(periodId); return null; });
    public static LongRunningPeriodicTask Start(long lastPeriodId, bool enabled, int msAfterWork, int msInitial, int msAfterResume, Func<long, int?> work)
      => new(lastPeriodId, enabled, msAfterWork, msInitial, msAfterResume, periodId => { return Task.FromResult(work(periodId)); });
    public static LongRunningPeriodicTask Start(long lastPeriodId, bool enabled, int msAfterWork, int msInitial, int msAfterResume, Action<long> work)
      => new(lastPeriodId, enabled, msAfterWork, msInitial, msAfterResume, periodId => { work(periodId); return Task.FromResult<int?>(null); });

    private LongRunningPeriodicTask(long lastPeriodId, bool enabled, int msAfterWork, int msInitial, int msAfterResume, Func<long, Task<int?>> work)
    {
      _lastPeriodId = lastPeriodId;
      DelayMsAfterWork = msAfterWork;
      DelayMsInitial = msInitial;
      DelayMsAfterEnabling = msAfterResume;
      Enabled = enabled;
      _thread = new Thread(() =>
      {
        sleep(DelayMsInitial);
        while (true)
        {
          if (!Enabled)
          {
            while (!Enabled)
            {
              Thread.Sleep(1000);
            }
            sleep(DelayMsAfterEnabling);
          }
          long rid = ++_lastPeriodId;
          int? delay = work(rid).ThrowMainThread().Result;
          sleep(delay ?? DelayMsAfterWork);
        }
      })
      { IsBackground = true };
      _thread.Start();
    }
  }
}
