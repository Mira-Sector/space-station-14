using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeRadio()
    {
        SubscribeLocalEvent<TeleframeConsoleRadioComponent, TelescienceFrameConsoleRelayEvent<TelescienceFrameTeleportFailedEvent>>(OnRadioTeleportFailed);
    }

    private void OnRadioTeleportFailed(Entity<TeleframeConsoleRadioComponent> ent, ref TelescienceFrameConsoleRelayEvent<TelescienceFrameTeleportFailedEvent> args)
    {
        SendRadioMessage(ent, args.Args.Reason);
    }

    protected virtual void SendRadioMessage(Entity<TeleframeConsoleRadioComponent> ent, string message)
    {
    }
}
