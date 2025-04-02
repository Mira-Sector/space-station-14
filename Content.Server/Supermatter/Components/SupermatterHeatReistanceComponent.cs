namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterHeatResistanceComponent : Component
{
    [DataField]
    public float BaseHeatResistance = 313.15f;

    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public float HeatResistance;
}
