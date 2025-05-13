using Robust.Shared.GameStates;

namespace Content.Shared.Glasses;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GlassesOverlayComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Shader = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public Color Color;
}
