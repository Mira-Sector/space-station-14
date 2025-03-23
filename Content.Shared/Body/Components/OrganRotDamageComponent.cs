using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganRotDamageComponent : Component
{
    [DataField(required: true)]
    public OrganRotDamageMode Mode;

    [ViewVariables, AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public int MinStage = 2;

    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(3f);

    [ViewVariables]
    public TimeSpan NextDamage;

    [DataField("damage")]
    public DamageSpecifier MaxDamage = new();

    [ViewVariables, AutoNetworkedField]
    public DamageSpecifier Damage = new();
}

[Serializable, NetSerializable]
public enum OrganRotDamageMode
{
    Rotted,
    Rotting
}
