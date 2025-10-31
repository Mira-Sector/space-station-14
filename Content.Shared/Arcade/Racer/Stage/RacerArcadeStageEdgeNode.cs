using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageEdgeNode : IRacerArcadeStageRenderableEdge
{
    [DataField(required: true)]
    public string ConnectionId;

    [DataField(required: true)]
    public List<Vector2> ControlPoints { get; set; }

    [DataField(required: true)]
    public float Width { get; set; }

    [DataField(required: true)]
    public SpriteSpecifier Texture { get; set; }
}
