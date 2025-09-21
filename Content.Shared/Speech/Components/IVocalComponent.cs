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
    Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? Sounds { get; set; }

    EntProtoId ScreamId { get; set; }

    SoundSpecifier Wilhelm { get; set; }

    float WilhelmProbability { get; set; }

    EntProtoId? ScreamAction { get; set; }

    EntityUid? ScreamActionEntity { get; set; }

    /// <summary>
    ///     Currently loaded emote sounds prototype, based on entity sex.
    ///     Null if no valid prototype for entity sex was found.
    /// </summary>
    ProtoId<EmoteSoundsPrototype>? EmoteSounds { get; set; }
}
