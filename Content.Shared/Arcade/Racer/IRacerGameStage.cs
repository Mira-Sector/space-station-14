using Content.Shared.Arcade.Racer.Stage;

namespace Content.Shared.Arcade.Racer;

public interface IRacerGameStage
{
    RacerGameStageSkyData Sky { get; set; }

    RacerArcadeStageGraph Graph { get; set; }
}
