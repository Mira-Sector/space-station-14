namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterModifyHeatResistanceOnMolesComponent : Component
{
    [DataField]
    public SupermatterModifyHeatResistanceOnMoles[] Resistances;
}

[DataDefinition]
public sealed partial class SupermatterModifyHeatResistanceOnMoles
{
    [DataField]
    public float MinMoles = float.MinValue;

    [DataField]
    public float MaxMoles = float.MaxValue;

    [DataField]
    public float HeatResistance;
}
