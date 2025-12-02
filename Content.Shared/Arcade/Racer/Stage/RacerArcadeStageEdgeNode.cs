using Content.Shared.Arcade.Racer.CollisionShapes;
using Content.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;
using Quaternion = Robust.Shared.Maths.Quaternion;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageEdgeNode : IRacerArcadeStageRenderableEdge
{
    [DataField(required: true)]
    public string ConnectionId;

    [DataField(required: true)]
    public Vector3[] ControlPoints { get; set; }

    [DataField(required: true)]
    public float Width { get; set; }

    [DataField]
    public ProtoId<RacerGameEdgeTexturePrototype>? Texture { get; set; }

    public const int CollisionSamples = 32;
    public const float CollisionThickness = 0.25f;

    public IEnumerable<BaseRacerArcadeObjectCollisionShape> GetCollisionShapes(RacerArcadeStageGraph graph, RacerArcadeStageNode parent)
    {
        if (!graph.TryGetNode(ConnectionId, out var next))
            yield break;

        var cp = this.GetWorldSpaceEdgePoints(parent.Position, next.Position);
        var sampled = RacerArcadeStageGraphHelpers.SampleBezier(cp, CollisionSamples);
        var halfWidth = Width * 0.5f;

        for (var i = 1; i < sampled.Length; i++)
        {
            var p0 = sampled[i - 1];
            var p1 = sampled[i];
            var delta = p1 - p0;

            var length = delta.Length;
            var halfLength = length * 0.5f;
            var dir = delta / length;
            var center = (p0 + p1) * 0.5f;

            var up = Vector3.UnitZ;
            var rotation = Quaternion.LookRotation(ref dir, ref up);
            yield return new Box
            {
                Box3 = new Box3(
                    -halfWidth, -halfWidth, -CollisionThickness,
                    halfLength, halfWidth, CollisionThickness
                ),
                Rotation = rotation,
                Center = center
            };
        }
    }
}
