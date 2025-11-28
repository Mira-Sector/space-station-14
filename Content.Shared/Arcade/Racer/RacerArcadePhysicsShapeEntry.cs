using Content.Shared.Arcade.Racer.PhysShapes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Arcade.Racer;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RacerArcadePhysicsShapeEntry
{
    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadePhysicsFlags>))]
    public int Layer = (int)RacerArcadePhysicsGroups.All;

    [DataField(customTypeSerializer: typeof(FlagSerializer<RacerArcadePhysicsFlags>))]
    public int Mask = (int)RacerArcadePhysicsGroups.All;

    [DataField(required: true)]
    public BaseRacerArcadeObjectPhysShape Shape;
}
