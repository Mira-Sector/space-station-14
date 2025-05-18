using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed partial class ModSuitSealButtonMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Part;
    public readonly bool ShouldSeal;

    public ModSuitSealButtonMessage(NetEntity part, bool shouldSeal)
    {
        Part = part;
        ShouldSeal = shouldSeal;
    }
}
