using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncSlaveMasterMessage : BoundUserInterfaceMessage
{
    public NetEntity Master;

    public SiliconSyncSlaveMasterMessage(NetEntity master)
    {
        Master = master;
    }
}
