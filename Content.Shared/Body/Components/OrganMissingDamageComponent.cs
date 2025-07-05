using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganMissingDamageComponent : Component
{
    [DataField]
    public OrganMissingDamageEntry[] Entries;
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
}
