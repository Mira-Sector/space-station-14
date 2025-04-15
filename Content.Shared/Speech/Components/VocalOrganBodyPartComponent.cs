using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VocalOrganBodyPartComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public uint Organs;
}
