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
public abstract partial class BaseElevatorEntitiesTeleportEvent : BaseElevatorTeleportEvent
{
    public HashSet<NetEntity> Entities;

    public BaseElevatorEntitiesTeleportEvent(HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(sourceMap, targetMap)
    {
        Entities = entities;
    }

    public BaseElevatorEntitiesTeleportEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs.SourceMap, baseArgs.TargetMap)
    {
        Entities = baseArgs.Entities;
    }
}

/// <summary>
///     Raised on the entrance incase it wishes to delay the teleportation logic.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorAttemptTeleportEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorAttemptTeleportEvent(HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorAttemptTeleportEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance that it is teleporting.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorTeleportingEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportingEvent (HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportingEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the exit so it will do the teleporting.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorTeleportEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportEvent(HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance that the teleportation was successfull.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorTeleportedEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportedEvent(HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportedEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance when teleporting to see their relative coords.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorGetEntityOffsetsEvent : BaseElevatorEntitiesTeleportEvent
{
    public Dictionary<NetEntity, Vector2> Offsets = new();

    public ElevatorGetEntityOffsetsEvent(HashSet<NetEntity> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorGetEntityOffsetsEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on an entity that got teleported by the elevator.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ElevatorGotTeleportedEvent : BaseElevatorTeleportEvent
{
    public ElevatorGotTeleportedEvent(MapId sourceMap, MapId targetMap) : base(sourceMap, targetMap)
    {
    }
}
