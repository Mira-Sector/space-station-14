using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiTurretComponent : Component
{
    [DataField(required: true)]
    public List<StationAiTurret> Modes = new();

    [ViewVariables]
    public int CurrentMode;

    [ViewVariables]
    public HashSet<ProtoId<NpcFactionPrototype>>? OldFactions;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class StationAiTurret
{
    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public LocId Tooltip;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>>? Factions;
}

