using Content.Shared.Body.Damage.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Body.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RespirationDelayOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public BodyDamageState TargetState = BodyDamageState.Dead;
}
