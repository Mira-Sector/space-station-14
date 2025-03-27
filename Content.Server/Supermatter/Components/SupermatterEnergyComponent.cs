namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyComponent : Component
{
    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public float CurrentEnergy
    {
        get => CurrentEnergy;
        set => Math.Max(value, 0f);
    }
}
