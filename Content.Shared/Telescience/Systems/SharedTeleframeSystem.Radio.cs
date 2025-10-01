using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeRadio()
    {
        SubscribeLocalEvent<TeleframeConsoleRadioComponent, TeleframeToConsoleRelayEvent<TeleframeTeleportFailedEvent>>(OnRadioTeleportFailed);
    }

    /// <summary>
    /// Sends message for teleport failiures, reason provided beforehand.
    /// </summary>
    private void OnRadioTeleportFailed(Entity<TeleframeConsoleRadioComponent> ent, ref TeleframeToConsoleRelayEvent<TeleframeTeleportFailedEvent> args)
    {
        SendRadioMessage(ent, args.Args.Reason);
    }

    // See server-side 
    protected virtual void SendRadioMessage(Entity<TeleframeConsoleRadioComponent> ent, string message)
    {
    }
}
