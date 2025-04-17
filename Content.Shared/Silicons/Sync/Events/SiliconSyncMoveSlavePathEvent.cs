using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncMoveSlavePathEvent : EntityEventArgs
{
    public NetEntity Master;
    public NetEntity Slave;
    public KeyValuePair<NetCoordinates, Direction>[] Path;

    public SiliconSyncMoveSlavePathEvent(NetEntity master, NetEntity slave, KeyValuePair<NetCoordinates, Direction>[] path)
    {
        Master = master;
        Slave = slave;
        Path = path;
    }
}
