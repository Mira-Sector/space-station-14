using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Events;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly string ContainerId = "pipe-crawling";

    protected EntityQuery<PipeCrawlingPipeComponent> PipeQuery;
    protected EntityQuery<PipeCrawlingEnterPointComponent> EnterQuery;
    protected EntityQuery<PipeCrawlingAutoPilotComponent> AutoQuery;
    protected EntityQuery<PipeCrawlingVisualsComponent> VisualsQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAction();
        InitializeEntry();
        InitializeVisuals();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnCrawlingInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnCrawlingRemove);
        SubscribeLocalEvent<PipeCrawlingComponent, GetVisMaskEvent>(OnCrawlingVisMask);

        SubscribeNetworkEvent<PipeCrawlingGetWishDirEvent>(OnGetWishDir);
        SubscribeNetworkEvent<PipeCrawlingSendWishDirEvent>(OnSendWishDir);

        PipeQuery = GetEntityQuery<PipeCrawlingPipeComponent>();
        EnterQuery = GetEntityQuery<PipeCrawlingEnterPointComponent>();
        AutoQuery = GetEntityQuery<PipeCrawlingAutoPilotComponent>();
        VisualsQuery = GetEntityQuery<PipeCrawlingVisualsComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<PipeCrawlingComponent, CanEnterPipeCrawlingComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var crawling, out var crawler, out var input))
        {
            if (crawling.NextMove > _timing.CurTime)
                continue;

            if (!input.CanMove)
            {
                crawling.NextMove += crawler.MoveDelay;
                Dirty(uid, crawling);
                continue;
            }

            crawling.NextMove += crawler.MoveDelay;

            if (AutoQuery.TryComp(uid, out var autoPilot))
                UpdateAuto((uid, autoPilot, crawling, crawler));

            if (_net.IsServer)
            {
                if (!_player.TryGetSessionByEntity(uid, out var session))
                    continue;

                var ev = new PipeCrawlingGetWishDirEvent(GetNetEntity(uid));
                RaiseNetworkEvent(ev, session);
                Dirty(uid, crawling);
            }
            else if (_net.IsClient)
            {
                CrawlPipe((uid, crawling, crawler, input, autoPilot), GetWishDir((uid, input)));
            }
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

    private void OnGetWishDir(PipeCrawlingGetWishDirEvent args)
    {
        var entity = GetEntity(args.NetEntity);
        var ev = new PipeCrawlingSendWishDirEvent(args.NetEntity, GetWishDir(entity));
        RaiseNetworkEvent(ev);
    }

    private void OnSendWishDir(PipeCrawlingSendWishDirEvent args)
    {
        var entity = GetEntity(args.NetEntity);
        if (!TryComp<PipeCrawlingComponent>(entity, out var crawling))
            return;

        if (!TryComp<CanEnterPipeCrawlingComponent>(entity, out var crawler))
            return;

        if (crawling.NextMove - crawler.MoveDelay > _timing.CurTime)
            return;

        CrawlPipe((entity, crawling), args.WishDir);
    }

    private Vector2 GetWishDir(Entity<InputMoverComponent?, EyeComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, false))
            return Vector2.Zero;

        var eyeRot = ent.Comp2.Rotation;
        return eyeRot.RotateVec(ent.Comp1.WishDir);
    }

    private void CrawlPipe(Entity<PipeCrawlingComponent, CanEnterPipeCrawlingComponent?, InputMoverComponent?, PipeCrawlingAutoPilotComponent?> ent, Vector2 wishDir)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2, ref ent.Comp3, false))
            return;

        if (wishDir == Vector2.Zero)
            return;

        if (!PipeQuery.TryComp(ent.Comp1.CurrentPipe, out var pipe))
        {
            Log.Warning($"{ToPrettyString(ent.Comp1.CurrentPipe)} does not have a {nameof(PipeCrawlingPipeComponent)} yet {ToPrettyString(ent.Owner)} is crawling inside?");
            Dirty(ent.Owner, ent.Comp1);
            return;
        }

        var normalizedWishDir = Vector2.Normalize(wishDir);
        var wishDirection = DirectionExtensions.GetDir(normalizedWishDir);

        if (Resolve(ent.Owner, ref ent.Comp4, false))
        {
            if (ent.Comp1.Direction == wishDirection)
                return;

            if (ent.Comp3.Sprinting)
            {
                // player requested a different direction
                // send them down that path
                var nextStop = GetNextStop((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, wishDirection);
                ent.Comp4.TargetPipe = nextStop;
                Dirty(ent.Owner, ent.Comp4);
                return;
            }

            // player requested finer control
            RemCompDeferred<PipeCrawlingAutoPilotComponent>(ent.Owner);
        }

        ent.Comp1.Direction = wishDirection;

        if (ent.Comp3.Sprinting)
        {
            var nextStop = GetNextStop((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, wishDirection);
            EnsureComp<PipeCrawlingAutoPilotComponent>(ent.Owner, out ent.Comp4);
            ent.Comp4.TargetPipe = nextStop;
            Dirty(ent.Owner, ent.Comp1);
            Dirty(ent.Owner, ent.Comp4);
            return;
        }

        if (!TryGetNextPipe((ent.Comp1.CurrentPipe, pipe), ent.Comp1.CurrentLayer, wishDirection, out var connection))
            return;

        TransferPipe((ent.Owner, ent.Comp1), connection.Value);
    }

    private void TransferPipe(Entity<PipeCrawlingComponent> ent, Entity<PipeCrawlingPipeComponent> pipe)
    {
        if (!_container.Insert(ent.Owner, pipe.Comp.Container))
            return;

        ent.Comp.CurrentPipe = pipe.Owner;
        EnableVisuals(ent);
        Dirty(ent);

        PlaySound(pipe);
    }

    protected virtual void PlaySound(Entity<PipeCrawlingPipeComponent> ent)
    {
    }

    private bool TryGetNextPipe(Entity<PipeCrawlingPipeComponent> ent, AtmosPipeLayer layer, Direction direction, [NotNullWhen(true)] out Entity<PipeCrawlingPipeComponent>? nextPipe)
    {
        nextPipe = null;

        if (!ent.Comp.ConnectedPipes.TryGetValue(layer, out var connections))
            return false;

        if (!connections.TryGetValue(direction, out var netConnection))
            return false;

        var connection = GetEntity(netConnection);
        var connectedPipe = PipeQuery.Comp(connection);
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
        EnableVisuals((toInsert, crawling));
        Dirty(toInsert, crawling);

        SetActionIcon((toInsert, crawling));

        _eye.RefreshVisibilityMask(toInsert);
    }

    private void Eject(Entity<PipeCrawlingPipeComponent> ent, EntityUid toRemove)
    {
        _container.Remove(toRemove, ent.Comp.Container);
        RemCompDeferred<PipeCrawlingComponent>(toRemove);
        _eye.RefreshVisibilityMask(toRemove);
    }
}
