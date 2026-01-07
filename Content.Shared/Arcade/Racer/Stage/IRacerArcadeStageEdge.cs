using Content.Shared.Arcade.Racer.CollisionShapes;

namespace Content.Shared.Arcade.Racer.Stage;

public interface IRacerArcadeStageEdge
{
    IEnumerable<BaseRacerArcadeObjectCollisionShape> GetCollisionShapes(RacerArcadeStageGraph graph, RacerArcadeStageNode parent)
    {
        yield break;
    }
}
