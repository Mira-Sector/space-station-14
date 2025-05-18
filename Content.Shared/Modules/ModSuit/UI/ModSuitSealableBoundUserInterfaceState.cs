using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitSealableBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<NetEntity, ModSuitSealableBuiEntry> Parts;

    public ModSuitSealableBoundUserInterfaceState(Dictionary<NetEntity, ModSuitSealableBuiEntry> parts)
    {
        Parts = parts;
    }
}

[Serializable, NetSerializable]
public sealed class ModSuitSealableBuiEntry
{
    public readonly Dictionary<bool, SpriteSpecifier> Sprite;
    public readonly bool IsSealed;

    public ModSuitSealableBuiEntry(Dictionary<bool, SpriteSpecifier> sprite, bool isSealed)
    {
        Sprite = sprite;
        IsSealed = isSealed;
    }
}
