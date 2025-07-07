namespace Content.Shared.Body.Damage.Components;

public abstract partial class BaseToggleOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public bool Enabled = true;
}
