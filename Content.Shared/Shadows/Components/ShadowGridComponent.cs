using Robust.Shared.GameStates;

namespace Content.Shared.Shadows.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedShadowSystem))]
public sealed partial class ShadowGridComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Casters = [];

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Vector2i, ShadowData> ShadowMap = [];
}
