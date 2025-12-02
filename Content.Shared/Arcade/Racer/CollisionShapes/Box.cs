using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

[Serializable, NetSerializable]
public sealed partial class Box : BaseRacerArcadeObjectCollisionShape
{
    [DataField("box")]
    public Box3 Box3 = Box3.UnitCentered;

    [DataField]
    public Quaternion Rotation;

    [DataField]
    public Vector3 Center;

    public override RacerArcadeObjectCollisionShapeComplexity Complexity => RacerArcadeObjectCollisionShapeComplexity.Box;

    public override Box3Rotated GetBox()
    {
        return new(Box3, Rotation, Center + Origin);
    }
}
