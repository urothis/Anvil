using System;
using Anvil.API.Events;

namespace Anvil.API.Events
{
  public sealed class OnDMPlayerDMLogin : IEvent
  {
    public NwPlayer DungeonMaster { get; internal init; } = null!;
    public string Password { get; internal init; } = null!;

    public bool Skip { get; set; }

    NwObject? IEvent.Context => DungeonMaster.LoginCreature;
  }
}

namespace Anvil.API
{
  public sealed partial class NwPlayer
  {
    /// <inheritdoc cref="Events.OnDMPlayerDMLogin"/>
    public event Action<OnDMPlayerDMLogin> OnDMPlayerDMLogin
    {
      add => EventService.Subscribe<OnDMPlayerDMLogin, DMEventFactory>(LoginCreature, value);
      remove => EventService.Unsubscribe<OnDMPlayerDMLogin, DMEventFactory>(LoginCreature, value);
    }
  }

  public sealed partial class NwModule
  {
    /// <inheritdoc cref="Events.OnDMPlayerDMLogin"/>
    public event Action<OnDMPlayerDMLogin> OnDMPlayerDMLogin
    {
      add => EventService.SubscribeAll<OnDMPlayerDMLogin, DMEventFactory>(value);
      remove => EventService.UnsubscribeAll<OnDMPlayerDMLogin, DMEventFactory>(value);
    }
  }
}
