using Robust.Shared.Audio;

namespace Content.Server.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiRequirePowerComponent : Component
{
    [ViewVariables]
    public bool IsPowered = true;

    [DataField]
    public float Wattage = 10f;

    [DataField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(4f);

    [ViewVariables]
    public TimeSpan LastWarning;

    [DataField]
    public SoundSpecifier? WarningSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");
}
