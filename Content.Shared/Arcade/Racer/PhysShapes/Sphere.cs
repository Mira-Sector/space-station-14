using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.PhysShapes;

[Serializable, NetSerializable]
public sealed partial class Sphere : BaseRacerArcadeObjectPhysShape
{
    [DataField]
    public float Radius = 0.5f;

    [DataField]
    public Vector3 Offset;

    public override RacerArcadeObjectPhysShapeComplexity Complexity => RacerArcadeObjectPhysShapeComplexity.Sphere;

    public override Box3Rotated GetBox()
    {
        var box = new Box3(Radius).Translate(Offset);
        return new(box, Origin);
    }
}
