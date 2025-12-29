using System.Numerics;

namespace Content.Shared.Atmos.Events;

[ByRefEvent]
public readonly record struct SpaceWindMovedEvent(Vector2 PushForce);
