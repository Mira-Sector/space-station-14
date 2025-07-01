using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganMissingDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage;
}
