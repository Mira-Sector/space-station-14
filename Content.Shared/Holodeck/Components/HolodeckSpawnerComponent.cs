using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holodeck.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolodeckSpawnerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityCoordinates Center;

    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Spawned = [];

    [ViewVariables, AutoNetworkedField]
    public HolodeckSpawnedScenarioData? SpawnedScenarioData;

    [DataField, AutoNetworkedField]
    public List<ProtoId<HolodeckScenarioPrototype>> Scenarios = [];
}
