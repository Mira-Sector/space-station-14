using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeRelay()
    {
        SubscribeLocalEvent<TeleframeComponent, TelescienceFrameTeleportedAllEvent>(RelayToConsole);
        SubscribeLocalEvent<TeleframeComponent, TelescienceFrameTeleportFailedEvent>(RelayToConsole);
    }

    protected void RelayToConsole<T>(Entity<TeleframeComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedConsole is not { } console)
            return;

        var ev = new TelescienceFrameConsoleRelayEvent<T>(ent, args);
        RaiseLocalEvent(console, ref ev);
    }

    protected void RelayToFrame<T>(Entity<TeleframeConsoleComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedTeleframe is not { } frame)
            return;

        var ev = new TelescienceConsoleFrameRelayEvent<T>(ent, args);
        RaiseLocalEvent(frame, ref ev);
    }
}
