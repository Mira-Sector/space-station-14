using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncMoveSlavePathEvent : EntityEventArgs
{
    public NetEntity Master;
    public NetEntity Slave;
    public SiliconSyncCommandingPathType PathType;
    public KeyValuePair<NetCoordinates, Direction>[] Path;

    public SiliconSyncMoveSlavePathEvent(NetEntity master, NetEntity slave, SiliconSyncCommandingPathType pathType, KeyValuePair<NetCoordinates, Direction>[]? path = null)
    {
        Master = master;
        Slave = slave;
        PathType = pathType;
        Path = path ?? Array.Empty<KeyValuePair<NetCoordinates, Direction>>();
    }
}
