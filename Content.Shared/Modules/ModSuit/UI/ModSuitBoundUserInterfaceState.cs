using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<BaseModSuitBuiEntry> Entries;

    public ModSuitBoundUserInterfaceState(List<BaseModSuitBuiEntry> entries)
    {
        Entries = entries;
    }
}
