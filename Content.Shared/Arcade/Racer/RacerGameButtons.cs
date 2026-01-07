using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
[Flags]
public enum RacerGameButtons : byte
{
    None = 0,

    PitchUp = 1 << 0,
    PitchDown = 1 << 1,

    TurnLeft = 1 << 2,
    TurnRight = 1 << 3,

    AirbrakeLeft = 1 << 4,
    AirbrakeRight = 1 << 5,

    Accelerate = 1 << 6,

    Pitching = PitchUp | PitchDown,
    Turning = TurnLeft | TurnRight,
    Airbraking = AirbrakeLeft | AirbrakeRight
}
