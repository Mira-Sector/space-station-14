using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holodeck.Ui;

[Serializable, NetSerializable]
public sealed partial class HolodeckSpawnerBoundUserInterfaceState(NetCoordinates center, List<ProtoId<HolodeckScenarioPrototype>> scenarios) : BoundUserInterfaceState
{
    public readonly NetCoordinates Center = center;
    public readonly List<ProtoId<HolodeckScenarioPrototype>> Scenarios = scenarios;
}
