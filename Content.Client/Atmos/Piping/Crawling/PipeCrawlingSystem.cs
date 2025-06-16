using System.Numerics;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Systems;
using Content.Shared.SubFloor;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.Atmos.Piping.Crawling;

public sealed partial class PipeCrawlingSystem : SharedPipeCrawlingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private PipeCrawlingOverlay _crawlingOverlay = default!;

    private const float VisualRange = 16;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetached);

        SubscribeLocalEvent<PipeCrawlingVisualsComponent, ComponentInit>(OnVisualsInit);
        SubscribeLocalEvent<PipeCrawlingVisualsComponent, ComponentRemove>(OnVisualsRemove);

        _crawlingOverlay = new();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PipeCrawlingVisualsComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, true);
        }
    }

    private void OnAttached(LocalPlayerAttachedEvent args)
    {
        UpdateOverlay(args.Entity);
    }

    private void OnDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay(args.Entity);
    }

    private void OnVisualsInit(Entity<PipeCrawlingVisualsComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, SubFloorVisuals.ScannerRevealed, true);
    }

    private void OnVisualsRemove(Entity<PipeCrawlingVisualsComponent> ent, ref ComponentRemove args)
    {
        _appearance.SetData(ent.Owner, SubFloorVisuals.ScannerRevealed, false);
    }

    protected override void UpdateOverlay(Entity<PipeCrawlingComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!IsLocalPlayer(ent!))
            return;

        if (!_overlay.HasOverlay<PipeCrawlingOverlay>())
            _overlay.AddOverlay(_crawlingOverlay);

        _crawlingOverlay.Crawler = ent!;
    }

    protected override void RemoveOverlay(Entity<PipeCrawlingComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!IsLocalPlayer(ent!))
            return;

        if (_crawlingOverlay.Crawler != ent!)
            return;

        _overlay.RemoveOverlay(_crawlingOverlay);
    }

    protected override void UpdateVisuals(Entity<PipeCrawlingComponent> ent)
    {
        if (!IsLocalPlayer(ent!))
            return;

        var toRemove = ent.Comp.PipeNet;
        ent.Comp.PipeNet.Clear();

        var currentPipe = (ent.Comp.CurrentPipe, PipeQuery.Comp(ent.Comp.CurrentPipe));
        foreach (var pipe in GetVisiblePipes(currentPipe))
        {
            EnsureComp<PipeCrawlingVisualsComponent>(pipe);

            ent.Comp.PipeNet.Add(pipe.Owner);
            toRemove.Remove(pipe);
        }

        Dirty(ent);

        UpdateOverlay(ent.AsNullable());

        foreach (var pipe in toRemove)
            RemComp<PipeCrawlingVisualsComponent>(pipe);
    }

    protected override void DisableVisuals(Entity<PipeCrawlingComponent> ent)
    {
        if (!IsLocalPlayer(ent!))
            return;

        foreach (var pipe in ent.Comp.PipeNet)
            RemComp<PipeCrawlingVisualsComponent>(pipe);

        ent.Comp.PipeNet.Clear();
        Dirty(ent);

        RemoveOverlay(ent.AsNullable());
    }

    private bool IsLocalPlayer(Entity<PipeCrawlingComponent> ent)
    {
        if (Player.LocalSession?.AttachedEntity != ent.Owner)
            return false;

        return true;
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
                    if (PipeQuery.TryComp(connection, out var pipe))
                        queue.Enqueue((connection, pipe));
                }
            }
        }
    }
}
