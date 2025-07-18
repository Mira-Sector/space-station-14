using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryUserInterfaceLinkedSinkComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Source;
}
