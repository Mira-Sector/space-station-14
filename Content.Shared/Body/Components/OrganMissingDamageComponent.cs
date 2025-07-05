using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganMissingDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public TimeSpan DamageDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan GraceTime = TimeSpan.FromSeconds(3);
}
