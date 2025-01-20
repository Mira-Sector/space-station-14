using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components;

/// <summary>
/// Gets added to the body to keep track of every limb that can get surgery
/// </summary>
/// <remarks>
/// Exists as the limbs are in a container within the body. We can never actually interact with them directly as a player.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryRecieverBodyComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntityUid, SurgeryRecieverComponent> Limbs = new();
}
