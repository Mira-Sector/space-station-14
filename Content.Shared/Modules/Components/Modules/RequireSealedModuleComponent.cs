using Content.Shared.Modules.ModSuit;
using Robust.Shared.GameStates;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RequireSealedModuleComponent : BaseToggleableModuleComponent
{
    [DataField, AutoNetworkedField]
    public HashSet<ModSuitPartType> Parts = [];

    [DataField, AutoNetworkedField]
    public bool RequireAll;

    [DataField, AutoNetworkedField]
    public bool EnableOnSealed;
}
