using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared.Arcade.Racer.Stage;

public interface IRacerArcadeStageRenderableEdge : IRacerArcadeStageEdge
{
    List<Vector2> ControlPoints { get; set; }

    float Width { get; set; }

    ProtoId<RacerGameEdgeTexturePrototype>? Texture { get; set; }
}
