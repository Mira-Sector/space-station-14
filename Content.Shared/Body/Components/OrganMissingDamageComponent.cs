using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(OrganMissingDamageSystem))]
public sealed partial class OrganMissingDamageComponent : Component
{
    [DataField]
    public OrganMissingDamageEntry[] Entries;

    [ViewVariables]
    public Dictionary<OrganMissingDamageType, int> DamageTypeCount = [];
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OrganMissingDamageEntry
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan GraceTime = TimeSpan.FromSeconds(3);

    [DataField]
    public OrganMissingDamageType DamageOn = OrganMissingDamageType.Missing;

    [DataField]
    public bool CapToOrganType;
}

[Serializable, NetSerializable]
public enum OrganMissingDamageType : byte
{
    Missing,
    Added
}
