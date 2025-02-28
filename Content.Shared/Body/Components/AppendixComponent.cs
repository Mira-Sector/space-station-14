using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AppendixComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Burst;

    [DataField(required: true)]
    public DamageSpecifier BurstDamage = new();

    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(3f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextDamage;

    [DataField]
    public LocId BurstExamine = "appendix-examine-burst";

    [DataField]
    public LocId NotBurstExamine = "appendix-examine-not-burst";
}
