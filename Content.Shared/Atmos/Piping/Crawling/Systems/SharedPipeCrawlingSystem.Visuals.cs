using System.Linq;
using System.Numerics;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.SubFloor;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public partial class SharedPipeCrawlingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float VisualRange = 16;

    private void InitializeVisuals()
    {
        SubscribeLocalEvent<PipeCrawlingVisualsComponent, ComponentInit>(OnVisualsInit);
        SubscribeLocalEvent<PipeCrawlingVisualsComponent, ComponentRemove>(OnVisualsRemove);
    }

    private void OnVisualsInit(Entity<PipeCrawlingVisualsComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, SubFloorVisuals.ScannerRevealed, true);
    }

    private void OnVisualsRemove(Entity<PipeCrawlingVisualsComponent> ent, ref ComponentRemove args)
    {
        _appearance.SetData(ent.Owner, SubFloorVisuals.ScannerRevealed, false);
    }

    private void EnableVisuals(Entity<PipeCrawlingComponent> ent)
    {
        List<Entity<PipeCrawlingVisualsComponent>> removed = [];
        foreach (var pipe in ent.Comp.PipeNet)
        {
            if (!VisualsQuery.TryComp(pipe, out var visuals))
                continue;

            visuals.Revealers.Remove(ent.Owner);
            removed.Add((pipe, visuals));
        }

        ent.Comp.PipeNet.Clear();

        var currentPipe = (ent.Comp.CurrentPipe, PipeQuery.Comp(ent.Comp.CurrentPipe));
        foreach (var pipe in GetVisiblePipes(currentPipe))
        {
            EnsureComp<PipeCrawlingVisualsComponent>(pipe, out var visuals);
            visuals.Revealers.Add(ent.Owner);
            Dirty(pipe.Owner, visuals);

            ent.Comp.PipeNet.Add(pipe.Owner);
        }

        Dirty(ent);

        foreach (var pipe in removed)
        {
            if (!pipe.Comp.Revealers.Any())
                RemCompDeferred<PipeCrawlingVisualsComponent>(pipe);
        }

        UpdateOverlay(ent);
    }

    private void DisableVisuals(Entity<PipeCrawlingComponent> ent)
    {
        foreach (var pipe in ent.Comp.PipeNet)
        {
            if (!VisualsQuery.TryComp(pipe, out var visuals))
                continue;

            visuals.Revealers.Remove(ent.Owner);
            if (!visuals.Revealers.Any())
                RemCompDeferred<PipeCrawlingVisualsComponent>(pipe);
        }

        ent.Comp.PipeNet.Clear();
        Dirty(ent);

        RemoveOverlay(ent);
    }

    protected virtual void UpdateOverlay(Entity<PipeCrawlingComponent> ent)
    {
    }

    protected virtual void RemoveOverlay(Entity<PipeCrawlingComponent> ent)
    {
    }

    private IEnumerable<Entity<PipeCrawlingPipeComponent>> GetVisiblePipes(Entity<PipeCrawlingPipeComponent> start)
    {
        var startPos = _transform.GetWorldPosition(start);

        HashSet<Entity<PipeCrawlingPipeComponent>> visited = [];

        Queue<Entity<PipeCrawlingPipeComponent>> queue = [];
        queue.Enqueue(start);

        while (queue.TryDequeue(out var currentPipe))
        {
            if (!visited.Add(currentPipe))
                continue;

            var pos = _transform.GetWorldPosition(currentPipe);
            var distance = Vector2.Distance(pos, startPos);
            if (distance > VisualRange)
                continue;

            yield return currentPipe;

            foreach (var (_, connections) in currentPipe.Comp.ConnectedPipes)
            {
                foreach (var (_, netConnection) in connections)
                {
                    var connection = GetEntity(netConnection);
                    var pipe = PipeQuery.Comp(connection);

                    queue.Enqueue((connection, pipe));
                }
            }
        }
    }
}
