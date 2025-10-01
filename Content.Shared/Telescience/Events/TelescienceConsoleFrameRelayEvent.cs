using Content.Shared.Telescience.Components;

namespace Content.Shared.Telescience.Events;

[ByRefEvent]
public readonly record struct TelescienceConsoleFrameRelayEvent<T>(Entity<TeleframeConsoleComponent> Console, T Args);
