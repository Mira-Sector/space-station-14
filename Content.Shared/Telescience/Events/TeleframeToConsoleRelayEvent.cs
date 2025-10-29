using Content.Shared.Telescience.Components;

namespace Content.Shared.Telescience.Events;

[ByRefEvent]
public readonly record struct TeleframeToConsoleRelayEvent<T>(Entity<TeleframeComponent> Frame, T Args);
