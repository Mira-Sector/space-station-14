using Content.Shared.Modules.ModSuit.UI.Modules;

namespace Content.Client.Modules.ModSuit.Modules;

public static class ModSuitModuleBuiHelper
{
    public static ModSuitModuleBaseModulePanel BuiEntryToPanel(NetEntity module, ModSuitModuleBaseModuleBuiEntry entry)
    {
        // order matters
        // more likely to be parented goes at the bottom
        return entry switch
        {
            ModSuitModuleBaseModuleBuiEntry baseEntry => new ModSuitModuleBaseModulePanel(module, baseEntry),
            _ => throw new NotImplementedException($"Tried to convert {entry.GetType()} to a panel which does not exist."),
        };
    }
}
