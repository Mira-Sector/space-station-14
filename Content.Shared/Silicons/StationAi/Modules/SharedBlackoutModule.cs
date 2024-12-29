using Content.Shared.Actions;

namespace Content.Shared.Silicons.StationAi.Modules;

public sealed partial class StationAiBlackoutEvent : InstantActionEvent
{
    [DataField(required: true)]
    public float Chance;
}
