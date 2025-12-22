using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Messages;

[Serializable, NetSerializable]
public sealed partial class RacerArcadeDebugFlagsChangedMessage(RacerArcadeDebugFlags flags) : EntityEventArgs
{
    public readonly RacerArcadeDebugFlags Flags = flags;
}
