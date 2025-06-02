using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitSealableBoundUserInterfaceState : BoundUserInterfaceState
{
    public KeyValuePair<NetEntity, ModSuitSealableBuiEntry>[] Parts;

    public ModSuitSealableBoundUserInterfaceState(KeyValuePair<NetEntity, ModSuitSealableBuiEntry>[] parts)
    {
        Parts = parts;
    }
}

[Serializable, NetSerializable]
public sealed class ModSuitSealableBuiEntry
{
    public readonly Dictionary<bool, SpriteSpecifier> Sprite;
    public readonly ModSuitPartType Type;
    public readonly bool IsSealed;

    public ModSuitSealableBuiEntry(Dictionary<bool, SpriteSpecifier> sprite, ModSuitPartType type, bool isSealed)
    {
        Sprite = sprite;
        Type = type;
        IsSealed = isSealed;
    }
}
