using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Announcements;

/// <summary>
/// Used for any announcements on the start of a round.
/// </summary>
[Prototype]
public sealed partial class RoundAnnouncementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("message")] public string? Message;

    /// <summary>
    ///     Absolute path for the announcement sound
    /// </summary>
    [DataField] public SoundSpecifier? AbsoluteSound;

    /// <summary>
    ///     Absolute path for the announcement sound
    /// </summary>
    [DataField] public string? RelativeSound;

}
