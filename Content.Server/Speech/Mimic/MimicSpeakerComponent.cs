namespace Content.Server.Speech.Mimic;

[RegisterComponent]
public sealed partial class MimicSpeakerComponent : Component
{
    [DataField(required: true)]
    public TimeSpan MinDelay;

    [DataField(required: true)]
    public TimeSpan MaxDelay;

    [ViewVariables]
    public TimeSpan NextMessage;
}
