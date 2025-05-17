using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit;

[Serializable, NetSerializable]
public sealed class ModSuitSealableBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly NetEntity Part;
    public readonly bool IsSealed;

    public ModSuitSealableBoundUserInterfaceState(NetEntity part, bool isSealed)
    {
        Part = part;
        IsSealed = isSealed;
    }
}
