namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterHeatEnergyComponent : Component
{
    [DataField]
    public float BaseEnergy;

    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public float CurrentEnergy;

    [DataField]
    public float MinTemperature;

    [DataField]
    public float SaturationTemperature;
}
