using Content.Shared.Chat.Prototypes;

namespace Content.Server.Speech;

public sealed partial class GetEmoteSoundsEvent : EntityEventArgs
{
    public EmoteSounds Sounds;

    public GetEmoteSoundsEvent(EmoteSounds sounds)
    {
        Sounds = sounds;
    }
}
