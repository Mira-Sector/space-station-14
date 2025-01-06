using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiHackableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Hacked = false;

    [DataField, AutoNetworkedField]
    public float Points = 10f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField]
    public SpriteSpecifier? RadialSprite;

    [DataField("radialTooltip")]
    public LocId? _radialTooltip;

    [ViewVariables]
    public string? RadialTooltip => _radialTooltip != null ? Loc.GetString(_radialTooltip) : null;

    [ViewVariables, AutoNetworkedField]
    public bool IsPowered;
}
