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

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, AnchorStateChangedEvent>(OnAnchored);
    }

    private void OnInit(EntityUid uid, PipeCrawlingPipeComponent component, ref ComponentInit args)
    {
        component.Enabled = UpdateState(uid, component);
    }

    private void OnAnchored(EntityUid uid, PipeCrawlingPipeComponent component, ref AnchorStateChangedEvent args)
    {
        component.Enabled = UpdateState(uid, component);
    }

    public bool UpdateState(EntityUid uid, PipeCrawlingPipeComponent? component = null, Direction sourceDir = Direction.Invalid)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!TryComp<TransformComponent>(uid, out var xform) || !xform.Anchored)
            return false;

        if (!TryComp<NodeContainerComponent>(uid, out var nodeComp))
            return false;

        // get the current pipes directions to itterate over
        PipeDirection? currentPipeDir = null;
        foreach (var node in nodeComp.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            currentPipeDir = pipe.CurrentPipeDirection;
            break;
        }

        if (currentPipeDir == null || currentPipeDir == PipeDirection.None)
            return false;

        var gridUid = xform.GridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var pipeCoords = _map.TileIndicesFor(gridUid!.Value, grid, xform.Coordinates);

        Dictionary<Direction, EntityUid> connectedPipes = new();
        foreach (PipeDirection pipeDir in Enum.GetValues(typeof(PipeDirection)))
        {
            //check if the flag isnt a bitwise flag
            if (pipeDir != PipeDirection.North &&
            pipeDir != PipeDirection.East &&
            pipeDir != PipeDirection.South &&
            pipeDir != PipeDirection.West)
                continue;

            if (pipeDir.ToDirection() == sourceDir)
                continue;

            if (!currentPipeDir.Value.HasFlag(pipeDir))
                continue;

            var dir = pipeDir.ToDirection();
            var pos = pipeCoords + dir.ToIntVec();

            bool foundPipe = false;

            foreach (var pipe in _map.GetAnchoredEntities(gridUid.Value, grid, pos))
            {
                if (component.ConnectedPipes.ContainsKey(dir))
                    break; // we can only have one match per direction. no pipe stacking :(

                if (pipe == uid)
                    continue;

                if (!TryComp<NodeContainerComponent>(pipe, out var currentNodeComp))
                    continue;

                var mainPipeDir = pipeDir.GetOpposite();

                foreach (var node in currentNodeComp.Nodes.Values)
                {
                    if (node is not PipeNode pipeNode)
                        continue;

                    if (!pipeNode.CurrentPipeDirection.HasDirection(mainPipeDir))
                        continue;

                    connectedPipes.Add(dir, pipe);
                    foundPipe = true;
                    break;
                }
            }

            if (!foundPipe)
                component.OpenPipeDir |= dir.AsFlag();
        }


        if (connectedPipes.Count <=0)
            return false;

        if (component.ConnectedPipes != connectedPipes)
        {
            component.ConnectedPipes = connectedPipes;

            foreach ((var dir, var pipe) in connectedPipes)
            {
                // update the connected pipes
                UpdateState(pipe, null, dir);
            }
        }

        Dirty(uid, component);
        return true;
    }
}
