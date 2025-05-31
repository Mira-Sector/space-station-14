using Content.Shared.Modules.ModSuit.UI.Modules;

namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitGetModuleUiEvent : EntityEventArgs
{
    public List<ModSuitBaseModuleBuiEntry> BuiEntries = [];
}
