using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncMoveSlaveLostEvent : EntityEventArgs
{
    public NetEntity Master;
    public NetEntity Slave;

    public SiliconSyncMoveSlaveLostEvent(NetEntity master, NetEntity slave)
    {
        Master = master;
        Slave = slave;
    }
}
