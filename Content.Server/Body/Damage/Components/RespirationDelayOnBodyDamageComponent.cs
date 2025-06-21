using Content.Shared.Body.Damage.Components;

namespace Content.Server.Body.Damage.Components;

[RegisterComponent]
public sealed partial class RespirationDelayOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public BodyDamageState TargetState = BodyDamageState.Dead;
}
