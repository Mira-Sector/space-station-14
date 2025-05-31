using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModuleContainedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Container;
}
