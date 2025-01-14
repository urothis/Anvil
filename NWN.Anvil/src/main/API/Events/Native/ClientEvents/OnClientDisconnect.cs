using System;
using System.Runtime.InteropServices;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Native.API;

namespace Anvil.API.Events
{
  /// <summary>
  /// Called when the player disconnects from the server.<br/>
  /// This event is also called if the player connects, and then disconnects on the character select screen.
  /// </summary>
  public sealed class OnClientDisconnect : IEvent
  {
    /// <summary>
    /// Gets the player that disconnected.
    /// </summary>
    public NwPlayer Player { get; private init; } = null!;

    NwObject? IEvent.Context => Player.ControlledCreature;

    internal sealed unsafe class Factory : HookEventFactory
    {
      private static FunctionHook<RemovePCFromWorldHook> Hook { get; set; } = null!;

      private delegate void RemovePCFromWorldHook(void* pServerExoAppInternal, void* pPlayer);

      protected override IDisposable[] RequestHooks()
      {
        delegate* unmanaged<void*, void*, void> pHook = &OnRemovePCFromWorld;
        Hook = HookService.RequestHook<RemovePCFromWorldHook>(pHook, FunctionsLinux._ZN21CServerExoAppInternal17RemovePCFromWorldEP10CNWSPlayer, HookOrder.Earliest);
        return new IDisposable[] { Hook };
      }

      [UnmanagedCallersOnly]
      private static void OnRemovePCFromWorld(void* pServerExoAppInternal, void* pPlayer)
      {
        ProcessEvent(new OnClientDisconnect
        {
          Player = CNWSPlayer.FromPointer(pPlayer).ToNwPlayer()!,
        });

        Hook.CallOriginal(pServerExoAppInternal, pPlayer);
      }
    }
  }
}

namespace Anvil.API
{
  public sealed partial class NwPlayer
  {
    /// <inheritdoc cref="Events.OnClientDisconnect"/>
    public event Action<OnClientDisconnect> OnClientDisconnect
    {
      add => EventService.Subscribe<OnClientDisconnect, OnClientDisconnect.Factory>(ControlledCreature, value);
      remove => EventService.Unsubscribe<OnClientDisconnect, OnClientDisconnect.Factory>(ControlledCreature, value);
    }
  }

  public sealed partial class NwModule
  {
    /// <inheritdoc cref="Events.OnClientDisconnect"/>
    public event Action<OnClientDisconnect> OnClientDisconnect
    {
      add => EventService.SubscribeAll<OnClientDisconnect, OnClientDisconnect.Factory>(value);
      remove => EventService.UnsubscribeAll<OnClientDisconnect, OnClientDisconnect.Factory>(value);
    }
  }
}
