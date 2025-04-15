using Content.Shared.Chat.Prototypes;

namespace Content.Shared.Speech;

public sealed partial class GetEmoteSoundsEvent : EntityEventArgs
{
    public EmoteSounds Sounds = new();

    public GetEmoteSoundsEvent(EmoteSounds sounds)
    {
        Sounds = sounds;
    }
}
