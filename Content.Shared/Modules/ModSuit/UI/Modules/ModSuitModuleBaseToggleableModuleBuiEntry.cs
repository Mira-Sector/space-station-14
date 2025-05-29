using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI.Modules;

[Serializable, NetSerializable]
[Virtual]
public partial class ModSuitModuleBaseToggleableModuleBuiEntry : ModSuitModuleBaseModuleBuiEntry
{
    public override int Priority => 1;

    public readonly bool Toggled;

    public ModSuitModuleBaseToggleableModuleBuiEntry(bool toggled)
    {
        Toggled = toggled;
    }
}
