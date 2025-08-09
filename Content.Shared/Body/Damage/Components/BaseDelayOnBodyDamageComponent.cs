namespace Content.Shared.Body.Damage.Components;

public abstract partial class BaseDelayOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public BodyDamageState TargetState = BodyDamageState.Dead;
}
