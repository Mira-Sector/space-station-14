using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.Prototypes;

[DataDefinition, Virtual, Serializable, NetSerializable]
public partial class EmoteSounds
{
    /// <summary>
    ///     Optional fallback sound that will play if collection
    ///     doesn't have specific sound for this emote id.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? FallbackSound;

    /// <summary>
    ///     Optional audio params that will be applied to ALL sounds.
    ///     This will overwrite any params that may be set in sound specifiers.
    /// </summary>
    [DataField("params")]
    public AudioParams? GeneralParams;

    /// <summary>
    ///     Collection of emote prototypes and their sounds.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<EmotePrototype>, SoundSpecifier> Sounds = new();
}
