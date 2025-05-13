using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Silicons.StationAi;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StationAiAnnounceOnTriggerComponent : Component
{
    [DataField]
    public TimeSpan AnnounceDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastAnnouncement;

    [DataField]
    public LocId Message;

    [DataField]
    public SoundSpecifier? Sound;
}
