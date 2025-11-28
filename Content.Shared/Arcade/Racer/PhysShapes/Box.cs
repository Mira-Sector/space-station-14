using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.PhysShapes;

[Serializable, NetSerializable]
public sealed partial class Box : BaseRacerArcadeObjectPhysShape
{
    [DataField("box")]
    public Box3 Box3 = Box3.UnitCentered;

    [DataField]
    public Quaternion Rotation;

    [DataField]
    public Vector3 Center;

    public override RacerArcadeObjectPhysShapeComplexity Complexity => RacerArcadeObjectPhysShapeComplexity.Box;

    public override Box3Rotated GetBox()
    {
        return new(Box3, Rotation, Center + Origin);
    }
}
