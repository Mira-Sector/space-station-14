using Content.Shared.Maths;
using Content.Shared.PolygonRenderer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

[Serializable, NetSerializable]
public sealed partial class Sphere : BaseRacerArcadeObjectCollisionShape
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [DataField]
    public float Radius = 0.5f;

    [DataField]
    public Vector3 Offset;

    private static readonly ProtoId<PolygonModelPrototype> DebugModelId = "RacerCollisionSphere";

    public override RacerArcadeObjectCollisionShapeComplexity Complexity => RacerArcadeObjectCollisionShapeComplexity.Sphere;

    public override Box3Rotated GetBox()
    {
        var box = new Box3(Radius).Translate(Offset);
        return new(box, Origin);
    }

    public override PolygonModel GetDebugModel(IPrototypeManager prototype)
    {
        var model = prototype.Index(DebugModelId);
        model.ModelMatrix = Matrix4.Scale(Radius) * Matrix4.CreateTranslation(Origin) * Matrix4.CreateTranslation(Offset);
        return model;
    }
}
