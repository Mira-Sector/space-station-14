using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncMoveSlaveToPositionEvent : EntityEventArgs
{
    public readonly NetCoordinates Coordinates;

    public readonly NetEntity Slave;
    public readonly NetEntity Master;

    public readonly bool MoveSlave;

    public SiliconSyncMoveSlaveToPositionEvent(NetCoordinates coordinates, NetEntity slave, NetEntity master, bool moveSlave)
    {
        Coordinates = coordinates;
        Slave = slave;
        Master = master;
        MoveSlave = moveSlave;
    }
}
