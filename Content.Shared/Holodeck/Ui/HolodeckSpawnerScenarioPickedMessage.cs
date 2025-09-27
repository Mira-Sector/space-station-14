using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holodeck.Ui;

[Serializable, NetSerializable]
public sealed partial class HolodeckSpawnerScenarioPickedMessage(ProtoId<HolodeckScenarioPrototype>? scenario) : BoundUserInterfaceMessage
{
    public readonly ProtoId<HolodeckScenarioPrototype>? Scenario = scenario;
}
