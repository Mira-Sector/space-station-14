namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterPowerTransmissionComponent : Component
{
    [DataField]
    public float BasePower;

    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public float CurrentPower;
}
