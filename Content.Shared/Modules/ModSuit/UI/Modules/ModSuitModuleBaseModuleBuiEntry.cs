using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI.Modules;

[Serializable, NetSerializable]
[Virtual]
public partial class ModSuitModuleBaseModuleBuiEntry
{
    public virtual int Priority => 0;

    public readonly int? Complexity;

    public ModSuitModuleBaseModuleBuiEntry(int? complexity)
    {
        Complexity = complexity;
    }
}
