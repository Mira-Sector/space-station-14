using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

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
}
