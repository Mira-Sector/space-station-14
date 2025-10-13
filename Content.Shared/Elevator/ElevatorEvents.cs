using Robust.Shared.Map;
using System.Numerics;

namespace Content.Shared.Elevator;

public abstract partial class BaseElevatorTeleportEvent(MapId sourceMap, MapId targetMap) : EntityEventArgs
{
    public MapId SourceMap = sourceMap;
    public MapId TargetMap = targetMap;
}

public abstract partial class BaseElevatorEntitiesTeleportEvent : BaseElevatorTeleportEvent
{
    public HashSet<EntityUid> Entities;

    public BaseElevatorEntitiesTeleportEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(sourceMap, targetMap)
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
public sealed partial class ElevatorAttemptTeleportEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorAttemptTeleportEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorAttemptTeleportEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance that it is teleporting.
/// </summary>
public sealed partial class ElevatorTeleportingEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportingEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportingEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the exit so it will do the teleporting.
/// </summary>
public sealed partial class ElevatorTeleportEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance that the teleportation was successfull.
/// </summary>
public sealed partial class ElevatorTeleportedEvent : BaseElevatorEntitiesTeleportEvent
{
    public ElevatorTeleportedEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorTeleportedEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on the entrance when teleporting to see their relative coords.
/// </summary>
public sealed partial class ElevatorGetEntityOffsetsEvent : BaseElevatorEntitiesTeleportEvent
{
    public Dictionary<EntityUid, Vector2> Offsets = [];

    public ElevatorGetEntityOffsetsEvent(HashSet<EntityUid> entities, MapId sourceMap, MapId targetMap) : base(entities, sourceMap, targetMap)
    {
    }

    public ElevatorGetEntityOffsetsEvent(BaseElevatorEntitiesTeleportEvent baseArgs) : base(baseArgs)
    {
    }
}

/// <summary>
///     Raised on an entity that got teleported by the elevator.
/// </summary>
public sealed partial class ElevatorGotTeleportedEvent(MapId sourceMap, MapId targetMap) : BaseElevatorTeleportEvent(sourceMap, targetMap)
{
}
