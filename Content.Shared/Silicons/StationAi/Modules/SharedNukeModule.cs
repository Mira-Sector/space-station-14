using Content.Shared.Actions;

namespace Content.Shared.Silicons.StationAi.Modules;

public sealed partial class StationAiNukeEvent : InstantActionEvent
{
    [DataField]
    public float AdditionalDelay;

    public StationAiNukeEvent(float delay)
    {
        AdditionalDelay = delay;
    }
}
