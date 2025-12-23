using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
[Flags]
public enum RacerArcadeDebugFlags : byte
{
    None = 0,
    ControlledData = 1 << 0,
    Collision = 1 << 1,

    // update as more are added/removed
    First = ControlledData,
    Last = Collision,
}
