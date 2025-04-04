namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterModifyIntegerityOnHeatResistanceComponent : Component
{
    [ViewVariables]
    public TimeSpan LastReaction;

    [DataField]
    public SupermatterModifyIntegerityOnHeatResistance[] Damages;
}

[DataDefinition]
public sealed partial class SupermatterModifyIntegerityOnHeatResistance
{
    /// <summary>
    /// Do we need to be below the heat resistance to deal damage or not
    /// </summary>
    [DataField]
    public bool BelowHeatResistance;

    [DataField]
    public float IntegerityDamage;
}
