using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DamageOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public BodyDamageState? ScaleToState;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamage;

    [DataField]
    public TimeSpan? DamageDelay;

    [DataField]
    public DamageOnBodyDamageModes Mode = DamageOnBodyDamageModes.Both;
}

[Serializable, NetSerializable]
[Flags]
public enum DamageOnBodyDamageModes : byte
{
    Healing = 1 << 0,
    Damage = 1 << 1,

    Both = Healing | Damage
}
