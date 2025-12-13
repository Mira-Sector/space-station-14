using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

[Serializable, NetSerializable]
public sealed partial class Sphere : BaseRacerArcadeObjectCollisionShape
{
    [DataField]
    public float Radius = 0.5f;

    [DataField]
    public Vector3 Offset;

    public override RacerArcadeObjectCollisionShapeComplexity Complexity => RacerArcadeObjectCollisionShapeComplexity.Sphere;

    public override Box3Rotated GetBox()
    {
        var box = new Box3(Radius).Translate(Offset);
        return new(box, Origin);
    }
}
