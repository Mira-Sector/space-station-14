using Robust.Shared.Serialization;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Arcade.Racer.Stage;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RacerArcadeStageNode
{
    [DataField]
    public List<IRacerArcadeStageEdge> Connections = [];

    [DataField(required: true)]
    public Vector3 Position;
}
