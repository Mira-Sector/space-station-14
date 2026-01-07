using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Flags, FlagsFor(typeof(RacerArcadeCollisionFlags))]
public enum RacerArcadeCollisionGroups : int
{
    None = 0,
    Track = 1 << 0,
    Vehicles = 1 << 1,
    All = int.MaxValue
}
