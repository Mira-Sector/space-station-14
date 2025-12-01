using Content.Shared.Arcade.Racer.PhysShapes;

namespace Content.Shared.Arcade.Racer.Stage;

public interface IRacerArcadeStageEdge
{
    IEnumerable<BaseRacerArcadeObjectPhysShape> GetPhysShapes(RacerArcadeStageGraph graph, RacerArcadeStageNode parent)
    {
        yield break;
    }
}
