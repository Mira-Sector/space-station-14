using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class OrganMissingDamageContainerComponent : Component
{
    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamage;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntityUid, OrganMissingDamageContainerEntry> Organs = [];
}

[Serializable, NetSerializable]
public record struct OrganMissingDamageContainerEntry(DamageSpecifier Damage, TimeSpan DamageGrace, TimeSpan DamageDelay, TimeSpan NextDamage)
{
    public readonly DamageSpecifier Damage = Damage;
    public readonly TimeSpan DamageGrace = DamageGrace;
    public readonly TimeSpan DamageDelay = DamageDelay;

    public TimeSpan NextDamage = NextDamage;
    public bool PassedDamageGrace;
}
