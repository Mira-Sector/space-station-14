using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holodeck;

[Serializable, NetSerializable]
public sealed partial class HolodeckSpawnedScenarioData
{
    [ViewVariables]
    public NetEntity Grid;

    [ViewVariables]
    public MapId MapId;

    [ViewVariables]
    public NetEntity Map;

    [ViewVariables]
    public ProtoId<HolodeckScenarioPrototype> Prototype;
}
