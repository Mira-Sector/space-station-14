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
    public Dictionary<EntityUid, OrganMissingDamageContainerEntry[]> Organs = [];

    [ViewVariables, AutoNetworkedField]
    public Dictionary<ProtoId<OrganPrototype>, DamageSpecifier> OrganTypeCaps = [];
}

[Serializable, NetSerializable]
public record struct OrganMissingDamageContainerEntry(DamageSpecifier Damage, TimeSpan DamageGrace, TimeSpan DamageDelay, TimeSpan NextDamage, ProtoId<OrganPrototype>? CapToOrganType)
{
    public readonly DamageSpecifier Damage = Damage;
    public readonly TimeSpan DamageGrace = DamageGrace;
    public readonly TimeSpan DamageDelay = DamageDelay;
    public readonly ProtoId<OrganPrototype>? CapToOrganType = CapToOrganType;

    public TimeSpan NextDamage = NextDamage;
    public bool PassedDamageGrace;
}
