using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed partial class ModSuitToggleButtonMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Module;
    public readonly NetEntity User;
    public readonly bool Toggle;

    public ModSuitToggleButtonMessage(NetEntity module, NetEntity user, bool toggle)
    {
        Module = module;
        User = user;
        Toggle = toggle;
    }
}
