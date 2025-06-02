namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyComponent : Component
{
    [ViewVariables, Access(typeof(SupermatterSystem), Other = AccessPermissions.Read)]
    public float CurrentEnergy
    {
        get => _currentEnergy;
        set => _currentEnergy = Math.Max(value, 0f);
    }

    private float _currentEnergy;
}
