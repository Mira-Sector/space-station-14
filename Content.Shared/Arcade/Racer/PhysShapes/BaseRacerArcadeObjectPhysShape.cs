using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.PhysShapes;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BaseRacerArcadeObjectPhysShape
{
    [DataField]
    public Vector3 Origin;

    public abstract RacerArcadeObjectPhysShapeComplexity Complexity { get; }

    public abstract Box3Rotated GetBox();
}
