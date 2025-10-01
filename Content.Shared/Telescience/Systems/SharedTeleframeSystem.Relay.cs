using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeRelay()
    {
        SubscribeLocalEvent<TeleframeComponent, TeleframeCanTeleportEvent>(RelayToConsole);
        SubscribeLocalEvent<TeleframeComponent, TeleframeTeleportFailedEvent>(RelayToConsole);
    }

    /// <summary>
    /// Relay InitialRelay's events on the Teleframe to its connected Console if it has one
    /// </summary>
    protected void RelayToConsole<T>(Entity<TeleframeComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedConsole is not { } console)
            return;

        var ev = new TeleframeToConsoleRelayEvent<T>(ent, args);
        RaiseLocalEvent(console, ref ev);
    }

    /// <summary>
    /// Relay InitialiseRelay's events on the Console to its connected Teleframe if it has one
    /// </summary>
    protected void RelayToFrame<T>(Entity<TeleframeConsoleComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedTeleframe is not { } frame)
            return;

        var ev = new TeleframeConsoleToFrameRelayEvent<T>(ent, args);
        RaiseLocalEvent(frame, ref ev);
    }
}
