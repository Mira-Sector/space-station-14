using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;


[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiShuntingComponent : Component
{
    [DataField("delay")]
    public float _delay = 3f;

    [ViewVariables]
    public TimeSpan Delay => TimeSpan.FromSeconds(_delay);

    [DataField]
    public SpriteSpecifier? Icon { get; set; }

    [DataField("tooltip")]
    public string? _tooltip { get; set; }

    [ViewVariables]
    public LocId? Tooltip => _tooltip != null ? Loc.GetString(_tooltip) : null;
}

[Serializable, NetSerializable]
public sealed class StationAiShuntingAttemptEvent : BaseStationAiAction
{
}

[Serializable, NetSerializable]
public sealed partial class StationAiShuntingEvent : SimpleDoAfterEvent
{
}
