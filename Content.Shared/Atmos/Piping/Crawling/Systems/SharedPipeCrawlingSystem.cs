using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] protected readonly ISharedPlayerManager Player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly string ContainerId = "pipe-crawling";

    protected EntityQuery<PipeCrawlingPipeComponent> PipeQuery;
    protected EntityQuery<PipeCrawlingEnterPointComponent> EnterQuery;
    protected EntityQuery<PipeCrawlingAutoPilotComponent> AutoQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAction();
        InitializeEntry();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnCrawlingInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnCrawlingRemove);
        SubscribeLocalEvent<PipeCrawlingComponent, GetVisMaskEvent>(OnCrawlingVisMask);
        SubscribeLocalEvent<PipeCrawlingComponent, MoveInputEvent>(OnCrawlingInput);

        PipeQuery = GetEntityQuery<PipeCrawlingPipeComponent>();
        EnterQuery = GetEntityQuery<PipeCrawlingEnterPointComponent>();
        AutoQuery = GetEntityQuery<PipeCrawlingAutoPilotComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<PipeCrawlingComponent, CanEnterPipeCrawlingComponent>();
        while (query.MoveNext(out var uid, out var crawling, out var crawler))
        {
            if (crawling.NextMove > _timing.CurTime)
                continue;

            crawling.NextMove += crawler.MoveDelay;

            if (AutoQuery.TryComp(uid, out var auto))
                UpdateAuto((uid, auto, crawling, crawler));

            UpdateVisuals((uid, crawling));

            if (crawling.WishDirection is not { } wishDir)
                continue;

            CrawlPipe((uid, crawling, crawler), wishDir);
        }
    }

    private void OnInit(Entity<PipeCrawlingPipeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, ContainerId);
    }

    private void OnRemove(Entity<PipeCrawlingPipeComponent> ent, ref ComponentRemove args)
    {
        foreach (var contained in ent.Comp.Container.ContainedEntities)
            Eject(ent, contained);
    }

    private void OnCrawlingInit(Entity<PipeCrawlingComponent> ent, ref ComponentInit args)
    {
        ent.Comp.LayerAction = _actions.AddAction(ent.Owner, ent.Comp.LayerActionId);
    }

    private void OnCrawlingRemove(Entity<PipeCrawlingComponent> ent, ref ComponentRemove args)
    {
        _actions.RemoveAction(ent.Comp.LayerAction);
        DisableVisuals(ent);
    }

    private void OnCrawlingVisMask(Entity<PipeCrawlingComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnCrawlingInput(Entity<PipeCrawlingComponent> ent, ref MoveInputEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        Direction? direction;
        if (args.HasDirectionalMovement && TryComp<InputMoverComponent>(ent.Owner, out var mover))
        {
            var (sprinting, walking) = _mover.GetVelocityInput(mover);
            var vec = sprinting.LengthSquared() > walking.LengthSquared() ? sprinting : walking;
            direction = Angle.FromWorldVec(vec).GetCardinalDir();
        }
        else
        {
            direction = null;
        }

        if (ent.Comp.WishDirection == direction)
            return;

        ent.Comp.WishDirection = direction;

        if (ent.Comp.NextMove > _timing.CurTime)
        {
            Dirty(ent);
            return;
        }

        CrawlPipe((ent.Owner, ent.Comp, null, args.Entity.Comp), direction);
    }

    private void CrawlPipe(Entity<PipeCrawlingComponent, CanEnterPipeCrawlingComponent?, InputMoverComponent?, PipeCrawlingAutoPilotComponent?> ent, Direction? direction)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2, ref ent.Comp3, false))
            return;

        if (!PipeQuery.TryComp(ent.Comp1.CurrentPipe, out var pipe))
        {
            Log.Warning($"{ToPrettyString(ent.Comp1.CurrentPipe)} does not have a {nameof(PipeCrawlingPipeComponent)} yet {ToPrettyString(ent.Owner)} is crawling inside?");
            Dirty(ent.Owner, ent.Comp1);
            return;
        }

        ent.Comp1.WishDirection = null;

        if (Resolve(ent.Owner, ref ent.Comp4, false))
        {
            if (ent.Comp1.LastDirection == direction)
            {
                Dirty(ent.Owner, ent.Comp1);
                return;
            }

            if (ent.Comp3.Sprinting)
            {
                if (direction == null)
                {
                    Dirty(ent.Owner, ent.Comp1);
                    return;
                }

                // player requested a different direction
                // send them down that path
                var nextStop = GetNextStop((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, direction.Value);
                ent.Comp4.TargetPipe = nextStop;
                Dirty(ent.Owner, ent.Comp4);
                Dirty(ent.Owner, ent.Comp1);
                return;
            }

            // player requested finer control
            RemCompDeferred<PipeCrawlingAutoPilotComponent>(ent.Owner);
        }

        if (direction != null)
            ent.Comp1.LastDirection = direction;

        Dirty(ent.Owner, ent.Comp1);

        if (ent.Comp3.Sprinting && direction != null)
        {
            var nextStop = GetNextStop((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, direction.Value);
            EnsureComp<PipeCrawlingAutoPilotComponent>(ent.Owner, out ent.Comp4);
            ent.Comp4.TargetPipe = nextStop;
            Dirty(ent.Owner, ent.Comp4);
            return;
        }

        if (!TryGetNextPipe((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, direction, out var connection))
            return;

        TransferPipe((ent.Owner, ent.Comp1), connection.Value);
    }

    private void TransferPipe(Entity<PipeCrawlingComponent> ent, Entity<PipeCrawlingPipeComponent> pipe)
    {
        _container.Insert(ent.Owner, pipe.Comp.Container);

        ent.Comp.CurrentPipe = pipe.Owner;
        Dirty(ent);

        PlaySound(pipe);
    }

    protected virtual void PlaySound(Entity<PipeCrawlingPipeComponent> ent)
    {
    }

    private bool TryGetNextPipe(Entity<PipeCrawlingPipeComponent> ent, AtmosPipeLayer layer, Direction? direction, [NotNullWhen(true)] out Entity<PipeCrawlingPipeComponent>? nextPipe)
    {
        nextPipe = null;

        if (direction == null)
            return false;

        if (!ent.Comp.ConnectedPipes.TryGetValue(layer, out var connections))
            return false;

        if (!connections.TryGetValue(direction.Value, out var netConnection))
            return false;

        var connection = GetEntity(netConnection);
        if (!PipeQuery.TryComp(connection, out var connectedPipe))
            return false;

        nextPipe = (connection, connectedPipe);
        return true;
    }

    private void Insert(Entity<PipeCrawlingPipeComponent> ent, EntityUid toInsert)
    {
        _container.Insert(toInsert, ent.Comp.Container);
        EnsureComp<PipeCrawlingComponent>(toInsert, out var crawling);
        crawling.NextMove = _timing.CurTime;
        crawling.CurrentPipe = ent.Owner;
        crawling.CurrentLayer = CompOrNull<AtmosPipeLayersComponent>(ent.Owner)?.CurrentPipeLayer ?? AtmosPipeLayer.Primary;

        _eye.RefreshVisibilityMask(toInsert);
        SetActionIcon((toInsert, crawling));
        Dirty(toInsert, crawling);
    }

    private void Eject(Entity<PipeCrawlingPipeComponent> ent, EntityUid toRemove)
    {
        _container.Remove(toRemove, ent.Comp.Container);
        _eye.RefreshVisibilityMask(toRemove);
        RemCompDeferred<PipeCrawlingComponent>(toRemove);
    }

    protected virtual void UpdateVisuals(Entity<PipeCrawlingComponent> ent)
    {
    }

    protected virtual void DisableVisuals(Entity<PipeCrawlingComponent> ent)
    {
    }

    protected virtual void UpdateOverlay(Entity<PipeCrawlingComponent?> ent)
    {
    }

    protected virtual void RemoveOverlay(Entity<PipeCrawlingComponent?> ent)
    {
    }
}
