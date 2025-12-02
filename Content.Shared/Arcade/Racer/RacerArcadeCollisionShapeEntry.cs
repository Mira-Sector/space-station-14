using Content.Shared.Arcade.Racer.CollisionShapes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Arcade.Racer;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RacerArcadeCollisionShapeEntry
{
    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadeCollisionFlags>))]
    public int Layer = (int)RacerArcadeCollisionGroups.All;

    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadeCollisionFlags>))]
    public int Mask = (int)RacerArcadeCollisionGroups.All;

    [DataField(required: true)]
    public BaseRacerArcadeObjectCollisionShape Shape;
}
