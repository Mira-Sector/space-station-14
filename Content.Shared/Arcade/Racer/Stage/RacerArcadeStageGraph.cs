using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageGraph : ISerializationHooks
{
    [DataField(required: true)]
    public Dictionary<string, RacerArcadeStageNode> Nodes = [];

    [DataField(required: true)]
    public string? StartingNode = null;

    public const int PhysicsMask = (int)RacerArcadePhysicsGroups.Track;
    public const int PhysicsLayer = (int)RacerArcadePhysicsGroups.Vehicles;

    public Box3 AABB { get; private set; }
    public List<RacerArcadePhysicsShapeEntry> PhysicsShapes { get; private set; }

    public bool TryGetStartingNode([NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        if (StartingNode is not { } starting)
        {
            node = null;
            return false;
        }

        return Nodes.TryGetValue(starting, out node);
    }

    public bool TryGetNode(string nodeId, [NotNullWhen(true)] out RacerArcadeStageNode? node)
    {
        return Nodes.TryGetValue(nodeId, out node);
    }

    void ISerializationHooks.AfterDeserialization()
    {
        AABB = Box3.Empty;
        PhysicsShapes = [];

        foreach (var (edge, parent) in this.GetConnections())
        {
            var shapes = edge.GetPhysShapes(this, parent);
            PhysicsShapes.EnsureCapacity(shapes.Count());
            foreach (var shape in shapes)
            {
                var entry = new RacerArcadePhysicsShapeEntry()
                {
                    Mask = PhysicsMask,
                    Layer = PhysicsLayer,
                    Shape = shape
                };
                PhysicsShapes.Add(entry);

                var shapeBox = shape.GetBox();
                var shapeAABB = shapeBox.CalcBoundingBox();
                AABB = new Box3(
                    Vector3.ComponentMin(AABB.LeftBottomBack, shapeAABB.LeftBottomBack),
                    Vector3.ComponentMax(AABB.RightTopFront, shapeAABB.RightTopFront)
                );
            }
        }
    }
}

