using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared.Arcade.Racer.Stage;

public interface IRacerArcadeStageRenderableEdge : IRacerArcadeStageEdge
{
    List<Vector2> ControlPoints { get; set; }

    float Width { get; set; }

    SpriteSpecifier Texture { get; set; }
}
