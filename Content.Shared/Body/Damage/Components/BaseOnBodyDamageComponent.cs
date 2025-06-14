namespace Content.Shared.Body.Damage.Components;

public abstract partial class BaseOnBodyDamageComponent : Component
{
    [DataField]
    public HashSet<BodyDamageState> RequiredStates = [];
}
