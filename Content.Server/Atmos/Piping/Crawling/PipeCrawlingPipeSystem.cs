using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.Crawling.Systems;

public sealed class PipeCrawlingPipeSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, AnchorStateChangedEvent>(OnAnchored);
    }

    private void OnAnchored(EntityUid uid, PipeCrawlingPipeComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateState(uid, component);

        if (Transform(uid).Anchored)
            return;

        foreach (var player in component.ContainedEntities)
        {
            RemComp<PipeCrawlingComponent>(player);
        }

    }

    public void UpdateState(EntityUid uid, PipeCrawlingPipeComponent? component = null, PipeDirection? currentPipeDir = null, EntityUid? updater = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        if (!TryComp<NodeContainerComponent>(uid, out var nodeComp))
            return;

        // get the current pipes directions to itterate over
        if (currentPipeDir == null)
        {
            currentPipeDir = PipeDirection.None;
            foreach (var node in nodeComp.Nodes.Values)
            {
                if (node is not PipeNode pipe)
                    continue;

                currentPipeDir |= pipe.CurrentPipeDirection;
            }
        }

        if (currentPipeDir == PipeDirection.None)
            return;

        var gridUid = xform.GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var pipeCoords = _map.TileIndicesFor(gridUid!.Value, grid, xform.Coordinates);

        Dictionary<Direction, EntityUid> connectedPipes = new();
        Dictionary<Direction, PipeDirection> connectedPipesDir = new();
        foreach (PipeDirection pipeDir in Enum.GetValues(typeof(PipeDirection)))
        {
            //check if the flag isnt a bitwise flag
            if (pipeDir != PipeDirection.North &&
            pipeDir != PipeDirection.East &&
            pipeDir != PipeDirection.South &&
            pipeDir != PipeDirection.West)
                continue;

            if (!currentPipeDir.Value.HasFlag(pipeDir))
                continue;

            var dir = pipeDir.ToDirection();
            var pos = pipeCoords + dir.ToIntVec();

            foreach (var pipe in _map.GetAnchoredEntities(gridUid.Value, grid, pos))
            {
                if (connectedPipes.ContainsKey(dir))
                    break; // we can only have one match per direction. no pipe stacking :(

                if (pipe == uid)
                    continue;

                if (!TryComp<NodeContainerComponent>(pipe, out var currentNodeComp))
                    continue;

                var mainPipeDir = pipeDir.GetOpposite();

                foreach (var node in currentNodeComp.Nodes.Values)
                {
                    if (node is PipeNode pipeNode &&
                        (pipeNode.CurrentPipeDirection == mainPipeDir || (pipeNode.CurrentPipeDirection & mainPipeDir) == mainPipeDir))
                    {
                        connectedPipes.Add(dir, pipe);
                        connectedPipesDir.Add(dir, pipeNode.CurrentPipeDirection);
                    }
                }
            }
        }

        if (connectedPipes.Count <=0)
            return;

        if (component.ConnectedPipes != connectedPipes)
        {
            component.ConnectedPipes = connectedPipes;

            if (updater != null)
                component.UpdatedBy.Add(updater.Value);

            Dirty(uid, component);


            foreach ((var dir, var pipe) in connectedPipes)
            {
                if (component.UpdatedBy.Contains(pipe))
                    continue;

                // update the connected pipes
                UpdateState(pipe, null, connectedPipesDir[dir], uid);
            }
        }
    }
}
