namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class DamageOnStepComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float RequiredMass = 50f;
}
