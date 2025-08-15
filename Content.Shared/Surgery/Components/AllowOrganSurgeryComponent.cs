using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AllowOrganSurgeryComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Organs = [];
}
