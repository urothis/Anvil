using System;
using System.Runtime.InteropServices;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Native.API;

namespace Anvil.API.Events
{
  public sealed class OnCalendarTimeChange : IEvent
  {
    public uint NewValue { get; private init; }

    public uint OldValue { get; private init; }
    public TimeChangeType TimeChangeType { get; private init; }

    NwObject? IEvent.Context => null;

    internal sealed unsafe class Factory : HookEventFactory
    {
      private static FunctionHook<UpdateTimeHook> Hook { get; set; } = null!;

      private delegate void UpdateTimeHook(void* pModule, uint nCalendarDay, uint nTimeOfDay, uint nUpdateDifference);

      protected override IDisposable[] RequestHooks()
      {
        delegate* unmanaged<void*, uint, uint, uint, void> pHook = &OnUpdateTime;
        Hook = HookService.RequestHook<UpdateTimeHook>(pHook, FunctionsLinux._ZN10CNWSModule10UpdateTimeEjjj, HookOrder.Earliest);
        return new IDisposable[] { Hook };
      }

      [UnmanagedCallersOnly]
      private static void OnUpdateTime(void* pModule, uint nCalendarDay, uint nTimeOfDay, uint nUpdateDifference)
      {
        CNWSModule module = CNWSModule.FromPointer(pModule);
        uint hour = module.m_nCurrentHour;
        uint day = module.m_nCurrentDay;
        uint month = module.m_nCurrentMonth;
        uint year = module.m_nCurrentYear;
        uint dayState = module.m_nTimeOfDayState;

        Hook.CallOriginal(pModule, nCalendarDay, nTimeOfDay, nUpdateDifference);

        if (hour != module.m_nCurrentHour)
        {
          ProcessEvent(new OnCalendarTimeChange
          {
            TimeChangeType = TimeChangeType.Hour,
            OldValue = hour,
            NewValue = module.m_nCurrentHour,
          });
        }

        if (day != module.m_nCurrentDay)
        {
          ProcessEvent(new OnCalendarTimeChange
          {
            TimeChangeType = TimeChangeType.Day,
            OldValue = day,
            NewValue = module.m_nCurrentDay,
          });
        }

        if (month != module.m_nCurrentMonth)
        {
          ProcessEvent(new OnCalendarTimeChange
          {
            TimeChangeType = TimeChangeType.Month,
            OldValue = month,
            NewValue = module.m_nCurrentMonth,
          });
        }

        if (year != module.m_nCurrentYear)
        {
          ProcessEvent(new OnCalendarTimeChange
          {
            TimeChangeType = TimeChangeType.Year,
            OldValue = year,
            NewValue = module.m_nCurrentYear,
          });
        }

        if (dayState != module.m_nTimeOfDayState)
        {
          ProcessEvent(new OnCalendarTimeChange
          {
            TimeChangeType = TimeChangeType.TimeOfDay,
            OldValue = dayState,
            NewValue = module.m_nTimeOfDayState,
          });
        }
      }
    }
  }
}

namespace Anvil.API
{
  public sealed partial class NwModule
  {
    /// <inheritdoc cref="Events.OnCalendarTimeChange"/>
    public event Action<OnCalendarTimeChange> OnCalendarTimeChange
    {
      add => EventService.SubscribeAll<OnCalendarTimeChange, OnCalendarTimeChange.Factory>(value);
      remove => EventService.UnsubscribeAll<OnCalendarTimeChange, OnCalendarTimeChange.Factory>(value);
    }
  }
}
