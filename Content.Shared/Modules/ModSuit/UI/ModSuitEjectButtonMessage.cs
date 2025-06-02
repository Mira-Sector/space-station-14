using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed partial class ModSuitEjectButtonMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Module;
    public readonly NetEntity User;
    public readonly NetEntity Container;

    public ModSuitEjectButtonMessage(NetEntity module, NetEntity user, NetEntity container)
    {
        Module = module;
        User = user;
        Container = container;
    }
}
