using Content.Shared.Modules.ModSuit.UI;

namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitGetUiEntriesEvent : EntityEventArgs
{
    public List<BaseModSuitBuiEntry> Entries = [];
}
