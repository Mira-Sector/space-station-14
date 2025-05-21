using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
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
    [Dependency] private readonly NPCSteeringSystem _steering = default!;

    internal Dictionary<EntityUid, Dictionary<EntityUid, (Task<PathResultEvent> Task, CancellationTokenSource Token, bool Move)>> Paths = [];

    internal const float PathRange = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        InitializeMonitor();

        SubscribeNetworkEvent<SiliconSyncMoveSlaveToPositionEvent>(OnSlaveCommanded);
    }

    public override void Update(float frameTime)
    {
        UpdateMonitor(frameTime);

        Dictionary<EntityUid, HashSet<EntityUid>> toRemove = [];

        foreach (var (master, tasks) in Paths)
        {
            foreach (var (slave, (task, token, move)) in tasks)
            {
                if (!task.IsCompleted)
                    continue;

                if (!task.IsCanceled)
                    TaskCompleted(master, slave, task, move);

                if (toRemove.TryGetValue(master, out var removeSlaves))
                {
                    removeSlaves.Add(slave);
                }
                else
                {
                    removeSlaves = [];
                    removeSlaves.Add(slave);
                    toRemove.Add(master, removeSlaves);
                }
            }
        }

        foreach (var (master, slaves) in toRemove)
        {
            var pathSlaves = Paths[master];

            foreach (var slave in slaves)
                pathSlaves.Remove(slave);

            if (!pathSlaves.Any())
                Paths.Remove(master);
        }
    }

    internal void TaskCompleted(EntityUid master, EntityUid slave, Task<PathResultEvent> task, bool moveSlave)
    {
        if (TerminatingOrDeleted(slave))
            return;

#pragma warning disable RA0004
        var result = task.Result;
#pragma warning restore RA0004

        var noPathEv = new SiliconSyncMoveSlavePathEvent(GetNetEntity(master), GetNetEntity(slave), SiliconSyncCommandingPathType.NoPath);

        if (result.Result != PathResult.Path)
        {
            RaiseNetworkEvent(noPathEv, master);
            return;
        }

        List<KeyValuePair<NetCoordinates, Direction>> tiles = new();
        tiles.Add(new KeyValuePair<NetCoordinates, Direction>(GetNetCoordinates(result.Path.First().Coordinates), Direction.Invalid));

        foreach (var node in result.Path.Skip(1))
        {
            var (lastTile, _) = tiles.Last();

            var offset = Vector2.Subtract(lastTile.Position, node.Coordinates.Position);
            var offsetDir = DirectionExtensions.GetDir(offset);

            tiles.Add(new KeyValuePair<NetCoordinates, Direction>(GetNetCoordinates(node.Coordinates), offsetDir));
        }

        tiles.Remove(tiles.First());

        if (!tiles.Any())
        {
            RaiseNetworkEvent(noPathEv, master);
            return;
        }

        var ev = new SiliconSyncMoveSlavePathEvent(GetNetEntity(master), GetNetEntity(slave), moveSlave ? SiliconSyncCommandingPathType.Moving : SiliconSyncCommandingPathType.PathFound, tiles.ToArray());
        RaiseNetworkEvent(ev, master);
        RaiseLocalEvent(ev);

        if (!moveSlave)
            return;

        var targetCoords = GetCoordinates(tiles.Last().Key);
        var steeringComp = _steering.Register(slave, targetCoords);
        steeringComp.CurrentPath = new Queue<PathPoly>(result.Path);
    }

    private void OnSlaveCommanded(SiliconSyncMoveSlaveToPositionEvent args)
    {
        var master = GetEntity(args.Master);
        var slaves = GetEntitySet(args.Slaves);

        if (!TryComp<SiliconSyncableMasterCommanderComponent>(master, out var commanderComp) || !commanderComp.Commanding.IsSupersetOf(slaves))
            return;

        if (commanderComp.NextCommand > Timing.CurTime)
            return;

        commanderComp.NextCommand += CommandUpdateRate;
        DirtyField(master, commanderComp, nameof(SiliconSyncableMasterCommanderComponent.NextCommand));

        foreach (var slave in slaves)
        {
            var token = new CancellationTokenSource();
            var task = _pathfinding.GetPathSafe(slave, Transform(slave).Coordinates, GetCoordinates(args.Coordinates), PathRange, token.Token);

            if (Paths.TryGetValue(master, out var paths))
            {
                if (!paths.ContainsKey(slave))
                    paths.Add(slave, (task, token, args.MoveSlave));
            }
            else
            {
                paths = [];
                paths.Add(slave, (task, token, args.MoveSlave));
                Paths.Add(master, paths);
            }
        }
    }
}
