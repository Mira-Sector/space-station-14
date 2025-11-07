using Robust.Shared.Prototypes;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Arcade.Racer.Stage;

public interface IRacerArcadeStageRenderableEdge : IRacerArcadeStageEdge
{
    List<Vector3> ControlPoints { get; set; }

    float Width { get; set; }

    ProtoId<RacerGameEdgeTexturePrototype>? Texture { get; set; }
}
