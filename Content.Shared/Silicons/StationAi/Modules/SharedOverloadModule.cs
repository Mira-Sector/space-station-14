using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Silicons.StationAi.Modules;

public sealed partial class StationAiOverloadEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public float Delay;

    [DataField(required: true)]
    public float BeepInterval;

    [DataField]
    public SoundSpecifier? BeepSound;
}
