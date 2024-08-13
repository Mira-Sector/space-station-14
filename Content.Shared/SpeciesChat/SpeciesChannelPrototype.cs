using Robust.Shared.Prototypes;

namespace Content.Shared.SpeciesChat;

[Prototype("speciesChannel")]
public sealed partial class SpeciesChannelPrototype : IPrototype
{
    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Single-character prefix to determine what channel a message should be sent to.
    /// </summary>
    public char Code { get; private set; } = '\0';

    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
