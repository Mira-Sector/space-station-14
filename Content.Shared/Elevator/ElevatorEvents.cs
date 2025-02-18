using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Elevator;

[Serializable, NetSerializable]
public abstract partial class BaseElevatorTeleportEvent : EntityEventArgs
{
    public NetEntity SourceMap;
    public NetEntity TargetMap;

    public BaseElevatorTeleportEvent(NetEntity sourceMap, NetEntity targetMap)
    {
        SourceMap = sourceMap;
        TargetMap = targetMap;
    }
}

[Serializable, NetSerializable]
public sealed partial class ElevatorTeleportEvent : BaseElevatorTeleportEvent
{
    public Dictionary<NetEntity, Vector2> Entities;

    public ElevatorTeleportEvent(Dictionary<NetEntity, Vector2> entities, NetEntity sourceMap, NetEntity targetMap) : base(sourceMap, targetMap)
    {
        Entities = entities;
    }
}

[Serializable, NetSerializable]
public sealed partial class ElevatorGotTeleportedEvent : BaseElevatorTeleportEvent
{
    public ElevatorGotTeleportedEvent(NetEntity sourceMap, NetEntity targetMap) : base(sourceMap, targetMap)
    {
    }
}
