namespace Content.Shared.Body.Events;

[ByRefEvent]
public readonly record struct BodyTrackerAdded(Entity<IComponent> Tracked, uint Count, string ComponentName);

[ByRefEvent]
public readonly record struct BodyTrackerRemoved(Entity<IComponent> Tracked, uint Count, string ComponentName);
