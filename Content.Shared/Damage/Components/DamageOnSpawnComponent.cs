namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class DamageOnSpawnComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool SplitLimbDamage = true;
}
