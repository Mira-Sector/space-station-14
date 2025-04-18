using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Sync.Events;

[Serializable, NetSerializable]
public sealed class SiliconSyncMoveSlaveToPositionEvent : EntityEventArgs
{
    public readonly NetCoordinates Coordinates;

    public readonly HashSet<NetEntity> Slaves;
    public readonly NetEntity Master;

    public readonly bool MoveSlave;

    public SiliconSyncMoveSlaveToPositionEvent(NetCoordinates coordinates, HashSet<NetEntity> slaves, NetEntity master, bool moveSlave)
    {
        Coordinates = coordinates;
        Slaves = slaves;
        Master = master;
        MoveSlave = moveSlave;
    }
}
