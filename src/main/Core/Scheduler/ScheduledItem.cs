using System;
using System.Collections.Generic;

namespace NWM.Core.Scheduler
{
  internal class ScheduledItem : IDisposable
  {
    public sealed class SortedByExecutionTime : IComparer<ScheduledItem>
    {
      public int Compare(ScheduledItem x, ScheduledItem y)
      {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;

        return x.ExecutionTime.CompareTo(y.ExecutionTime);
      }
    }

    private readonly SchedulerService schedulerService;
    private readonly Action task;

    public bool Repeating;
    public double ExecutionTime;
    public double Schedule;

    public ScheduledItem(SchedulerService schedulerService, Action task, double executionTime)
    {
      this.schedulerService = schedulerService;
      this.task = task;
      this.ExecutionTime = executionTime;
      Repeating = false;
    }

    public ScheduledItem(SchedulerService schedulerService, Action task, double executionTime, double schedule)
    {
      this.schedulerService = schedulerService;
      this.task = task;
      this.ExecutionTime = executionTime;
      this.Schedule = schedule;
      Repeating = true;
    }

    public void Reschedule(double newTime)
    {
      this.ExecutionTime = newTime;
    }

    public void Execute()
    {
      task();
    }

    public void Dispose()
    {
      schedulerService.Unschedule(this);
    }
  }
}