using Robust.Shared.GameStates;

namespace Content.Shared.Coughing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoughRotChangeModifyComponent : Component
{
    [DataField]
    public bool DisabledOnRot = true;

    [ViewVariables, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    public float HealthyMultiplier = 0.5f;

    [DataField]
    public float DamagedMultiplier = 2.5f;

    [ViewVariables, AutoNetworkedField]
    public float CurrentMutliplier;
}
