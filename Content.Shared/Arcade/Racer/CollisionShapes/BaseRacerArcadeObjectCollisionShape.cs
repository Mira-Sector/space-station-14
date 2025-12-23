using Content.Shared.Maths;
using Content.Shared.PolygonRenderer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.CollisionShapes;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BaseRacerArcadeObjectCollisionShape
{
    [DataField]
    public Vector3 Origin;

    public abstract RacerArcadeObjectCollisionShapeComplexity Complexity { get; }

    public abstract Box3Rotated GetBox();

    public abstract PolygonModel GetDebugModel(IPrototypeManager prototype);
}
