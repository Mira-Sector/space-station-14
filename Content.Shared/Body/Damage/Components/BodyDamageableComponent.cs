using Content.Shared.Body.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(BodyDamageableSystem))]
public sealed partial class BodyDamageableComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Damage;
}
