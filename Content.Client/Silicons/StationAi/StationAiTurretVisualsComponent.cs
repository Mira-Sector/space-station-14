using Content.Shared.Silicons.StationAi;
using Robust.Client.Animations;

namespace Content.Client.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiTurretVisualsComponent : SharedStationAiTurretVisualsComponent
{
    [DataField]
    public string OpeningSpriteState = "popup";

    [DataField]
    public string ClosingSpriteState = "popdown";

    [DataField]
    public string OpenSpriteState = "openTurretCover";

    [DataField]
    public string ClosedSpriteState = "turretCover";

    public Animation OpeningAnimation = default!;
    public Animation ClosingAnimation = default!;
}
