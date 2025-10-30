using Content.Shared.Arcade.Racer.Stage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer;

[Prototype]
public sealed partial class RacerGameStagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public RacerGameStageSkyData Sky = default!;

    [DataField(required: true)]
    public RacerArcadeStageGraph Graph = default!;
}
