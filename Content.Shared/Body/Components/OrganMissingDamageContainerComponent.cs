using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(OrganMissingDamageSystem))]
public sealed partial class OrganMissingDamageContainerComponent : Component
{
    public static readonly TimeSpan DefaultDamageDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan DamageDelay = DefaultDamageDelay;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamage;

    [ViewVariables, AutoNetworkedField]
    public List<OrganMissingDamageContainerEntry> Organs = [];

    [ViewVariables, AutoNetworkedField]
    public Dictionary<ProtoId<OrganPrototype>, DamageSpecifier> OrganTypeCaps = [];
}

[Serializable, NetSerializable]
public record struct OrganMissingDamageContainerEntry(NetEntity Organ, DamageSpecifier Damage, TimeSpan DamageGrace, TimeSpan DamageDelay, TimeSpan NextDamage, OrganMissingDamageType DamageOn, ProtoId<OrganPrototype> OrganType, bool CapToOrganType)
{
    public readonly NetEntity Organ = Organ;
    public readonly DamageSpecifier Damage = Damage;
    public readonly TimeSpan DamageGrace = DamageGrace;
    public readonly TimeSpan DamageDelay = DamageDelay;
    public readonly OrganMissingDamageType DamageOn = DamageOn;
    public readonly ProtoId<OrganPrototype> OrganType = OrganType;
    public readonly bool CapToOrganType = CapToOrganType;

    public TimeSpan NextDamage = NextDamage;
    public bool PassedDamageGrace;
}
