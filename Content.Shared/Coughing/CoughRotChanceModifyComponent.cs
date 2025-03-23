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
    public float HealthyMultiplier = 1f;

    [DataField]
    public float DamagedMultiplier = 6f;

    [ViewVariables, AutoNetworkedField]
    public float CurrentMutliplier;
}
