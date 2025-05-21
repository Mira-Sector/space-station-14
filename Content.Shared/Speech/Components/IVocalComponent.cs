using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

public interface IVocalComponent
{
    /// <summary>
    ///     Emote sounds prototype id for each sex (not gender). Entities without <see cref="HumanoidComponent"/> considered to be <see cref="Sex.Unsexed"/>.
    /// </summary>
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? Sounds { get; set; }

    public EntProtoId ScreamId { get; set; }

    public SoundSpecifier Wilhelm { get; set; }

    public float WilhelmProbability { get; set; }

    public EntProtoId? ScreamAction { get; set; }

    public EntityUid? ScreamActionEntity { get; set; }

    /// <summary>
    ///     Currently loaded emote sounds prototype, based on entity sex.
    ///     Null if no valid prototype for entity sex was found.
    /// </summary>
    public EmoteSoundsPrototype? EmoteSounds { get; set; }
}
