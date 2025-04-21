using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.XRay;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowXRayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true), AutoNetworkedField]
    public string Shader = string.Empty;

    // this can kill performance if too high
    [DataField, AutoNetworkedField]
    public float Range = 8;
}
