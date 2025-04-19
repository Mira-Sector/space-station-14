using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.XRay;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ShowXRayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true), AutoNetworkedField]
    public string Shader = string.Empty;

    [DataField, AutoNetworkedField]
    public float Range = 12f;

    [DataField, AutoNetworkedField]
    public TimeSpan RefreshTime = TimeSpan.FromSeconds(0.5f);

    /// <remarks>
    /// Not networked as this is done client side
    /// </remarks>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextRefresh;
}
