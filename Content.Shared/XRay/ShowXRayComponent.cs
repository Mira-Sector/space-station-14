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
    public float EntityRange = 8;

    [DataField, AutoNetworkedField]
    public float TileRange = 9; // higher as it works from the center
}
