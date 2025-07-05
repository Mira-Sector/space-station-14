using Content.Shared.Body.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoughOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [ViewVariables, AutoNetworkedField, Access(typeof(CoughOnBodyDamageSystem))]
    public float CurrentChance;

    [DataField]
    public float MinChance = 0f;

    [DataField]
    public float MaxChance = 1f;
}
