namespace SimpleBot
{
  internal class LongRunningPeriodicTask
  {
    public readonly Task _task;
    long _lastPeriodId;

    public int DelayMsAfterWork;
    public int DelayMsInitial;
    public int DelayMsAfterEnabling;

    public volatile bool Enabled;

    static Task sleep(int delayMs) => delayMs <= 0 ? Task.CompletedTask : Task.Delay(delayMs);

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
      _task = Task.Factory.StartNew(async () =>
      {
        await sleep(DelayMsInitial).ConfigureAwait(true);
        while (true)
        {
          if (!Enabled)
          {
            while (!Enabled)
            {
              await sleep(1000).ConfigureAwait(true);
            }
            await sleep(DelayMsAfterEnabling).ConfigureAwait(true);
          }
          long rid = ++_lastPeriodId;
          int? delay = await work(rid).ThrowMainThread();
          await sleep(delay ?? DelayMsAfterWork).ConfigureAwait(true);
        }
      }, TaskCreationOptions.LongRunning).ThrowMainThread();
    }
  }
}
