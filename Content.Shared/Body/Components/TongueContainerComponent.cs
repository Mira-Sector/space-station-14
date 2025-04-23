using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TongueContainerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public uint Tongues;
}
