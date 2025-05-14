using Robust.Shared.Audio;

namespace Content.Server.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiAnnounceOnTriggerComponent : Component
{
    [DataField]
    public LocId Message;

    [DataField]
    public SoundSpecifier? Sound;
}
