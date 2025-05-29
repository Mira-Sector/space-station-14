using Content.Shared.Modules.ModSuit.UI.Modules;
using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitModuleBoundtUserInterfaceState : BoundUserInterfaceState
{
    public KeyValuePair<NetEntity, ModSuitModuleBaseModuleBuiEntry>[] Modules;

    public ModSuitModuleBoundtUserInterfaceState(KeyValuePair<NetEntity, ModSuitModuleBaseModuleBuiEntry>[] modules)
    {
        Modules = modules;
    }
}
