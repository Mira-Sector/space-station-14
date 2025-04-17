using Content.Server.NPC.Pathfinding;
using Content.Shared.Silicons.Sync;
using Content.Shared.Silicons.Sync.Components;
using Content.Shared.Silicons.Sync.Events;
using Robust.Shared.Map;
using System.Numerics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Silicons.Sync;

public sealed partial class SiliconSyncSystem : SharedSiliconSyncSystem
{
    [Dependency] private readonly PathfindingSystem _pathfinding = default!;

    private Dictionary<EntityUid, Dictionary<EntityUid, (Task<PathResultEvent> Task, CancellationTokenSource Token)>> _paths = new();

    private const float PathRange = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SiliconSyncMoveSlaveToPositionEvent>(OnSlaveCommanded);
    }

    public override void Update(float frameTime)
    {
        Dictionary<EntityUid, HashSet<EntityUid>> toRemove = new();

        foreach (var (master, tasks) in _paths)
        {
            foreach (var (slave, (task, token)) in tasks)
            {
                if (!task.IsCompleted)
                    continue;

                if (!task.IsCanceled)
                    TaskCompleted(master, slave, task);

                if (toRemove.TryGetValue(master, out var removeSlaves))
                {
                    removeSlaves.Add(slave);
                }
                else
                {
                    removeSlaves = new();
                    removeSlaves.Add(slave);
                    toRemove.Add(master, removeSlaves);
                }
            }
        }

        foreach (var (master, slaves) in toRemove)
        {
            var pathSlaves = _paths[master];

            foreach (var slave in slaves)
                pathSlaves.Remove(slave);

            if (!pathSlaves.Any())
                _paths.Remove(master);
        }
    }

    private void TaskCompleted(EntityUid master, EntityUid slave, Task<PathResultEvent> task)
    {
#pragma warning disable RA0004
        var result = task.Result;
#pragma warning restore RA0004

        if (result.Result != PathResult.Path)
            return;

        List<KeyValuePair<NetCoordinates, Direction>> tiles = new();
        tiles.Add(new KeyValuePair<NetCoordinates, Direction>(GetNetCoordinates(Transform(slave).Coordinates), Direction.Invalid));

        foreach (var node in result.Path)
        {
            var (lastTile, _) = tiles.Last();

            var offset = Vector2.Subtract(lastTile.Position, node.Coordinates.Position);
            var offsetDir = DirectionExtensions.GetDir(offset);

            tiles.Add(new KeyValuePair<NetCoordinates, Direction>(GetNetCoordinates(node.Coordinates), offsetDir));
        }

        var ev = new SiliconSyncMoveSlavePathEvent(GetNetEntity(master), GetNetEntity(slave), tiles.ToArray());
        RaiseNetworkEvent(ev, master);
        RaiseLocalEvent(ev);
    }

    private void OnSlaveCommanded(SiliconSyncMoveSlaveToPositionEvent args)
    {
        var master = GetEntity(args.Master);
        var slave = GetEntity(args.Slave);

        if (!TryComp<SiliconSyncableMasterCommanderComponent>(master, out var commanderComp) || commanderComp.Commanding != slave)
            return;

        if (commanderComp.NextCommand > _timing.CurTime)
            return;

        commanderComp.NextCommand += CommandUpdateRate;
        DirtyField(master, commanderComp, nameof(SiliconSyncableMasterCommanderComponent.NextCommand));

        var token = new CancellationTokenSource();
        var task = _pathfinding.GetPathSafe(slave, Transform(slave).Coordinates, GetCoordinates(args.Coordinates), PathRange, token.Token);

        if (_paths.TryGetValue(master, out var paths))
        {
            if (!paths.ContainsKey(slave))
                paths.Add(slave, (task, token));
        }
        else
        {
            paths = new();
            paths.Add(slave, (task, token));
            _paths.Add(master, paths);
        }
    }
}
