using Content.Shared.Surgery;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.Prototypes;

/// <summary>
///     Sounds collection for each <see cref="EmotePrototype"/>.
///     Different entities may use different sounds collections.
/// </summary>
[Prototype, Serializable, NetSerializable]
public sealed partial class EmoteSoundsPrototype : EmoteSounds, IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
