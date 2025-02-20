using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Elevator;

[Serializable, NetSerializable]
public abstract partial class BaseElevatorTeleportEvent : EntityEventArgs
{
    public MapId SourceMap;
    public MapId TargetMap;

    public BaseElevatorTeleportEvent(MapId sourceMap, MapId targetMap)
    {
        SourceMap = sourceMap;
        TargetMap = targetMap;
    }
}

[Serializable, NetSerializable]
public sealed partial class ElevatorTeleportEvent : BaseElevatorTeleportEvent
{
    public Dictionary<NetEntity, Vector2> Entities;

    public ElevatorTeleportEvent(Dictionary<NetEntity, Vector2> entities, MapId sourceMap, MapId targetMap) : base(sourceMap, targetMap)
    {
        Entities = entities;
    }
}

[Serializable, NetSerializable]
public sealed partial class ElevatorGotTeleportedEvent : BaseElevatorTeleportEvent
{
    public ElevatorGotTeleportedEvent(MapId sourceMap, MapId targetMap) : base(sourceMap, targetMap)
    {
    }
}
