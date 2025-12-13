using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer;

[Prototype]
public sealed partial class RacerGameStagePrototype : BaseRacerGameStage, IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;
}
