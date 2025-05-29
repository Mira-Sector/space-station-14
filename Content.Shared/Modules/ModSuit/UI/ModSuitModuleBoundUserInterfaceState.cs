using Content.Shared.Modules.ModSuit.UI.Modules;
using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitModuleBoundUserInterfaceState : BoundUserInterfaceState
{
    public KeyValuePair<NetEntity, ModSuitModuleBaseModuleBuiEntry>[] Modules;

    public ModSuitModuleBoundUserInterfaceState(KeyValuePair<NetEntity, ModSuitModuleBaseModuleBuiEntry>[] modules)
    {
        Modules = modules;
    }
}
