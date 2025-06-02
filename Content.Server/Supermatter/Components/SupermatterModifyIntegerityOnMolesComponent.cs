namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterModifyIntegerityOnMolesComponent : Component
{
    [DataField]
    public SupermatterModifyIntegerityOnMoles[] Damages;
}

[DataDefinition]
public sealed partial class SupermatterModifyIntegerityOnMoles
{
    [DataField]
    public float MinMoles = float.MinValue;

    [DataField]
    public float MaxMoles = float.MaxValue;

    [DataField]
    public float IntegerityDamage;
}
