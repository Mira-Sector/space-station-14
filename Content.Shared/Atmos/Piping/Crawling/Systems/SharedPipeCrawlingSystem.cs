using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Atmos.Piping.Crawling.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string ContainerId = "pipe-crawling";

    protected EntityQuery<PipeCrawlingPipeComponent> PipeQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAction();
        InitializeEntry();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<PipeCrawlingComponent, ComponentInit>(OnCrawlingInit);
        SubscribeLocalEvent<PipeCrawlingComponent, ComponentRemove>(OnCrawlingRemove);

        SubscribeNetworkEvent<PipeCrawlingGetWishDirEvent>(OnGetWishDir);
        SubscribeNetworkEvent<PipeCrawlingSendWishDirEvent>(OnSendWishDir);

        PipeQuery = GetEntityQuery<PipeCrawlingPipeComponent>();
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

            if (_net.IsServer)
            {
                if (!_player.TryGetSessionByEntity(uid, out var session))
                {
                    crawling.NextMove += crawler.MoveDelay;
                    Dirty(uid, crawling);
                    continue;
                }

                var ev = new PipeCrawlingGetWishDirEvent(GetNetEntity(uid));
                RaiseNetworkEvent(ev, session);
            }
            else if (_net.IsClient)
            {
                CrawlPipe((uid, crawling, crawler), GetWishDir((uid, input)));
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

        if (crawling.NextMove > _timing.CurTime)
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

    private void CrawlPipe(Entity<PipeCrawlingComponent, CanEnterPipeCrawlingComponent?> ent, Vector2 wishDir)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2, false))
            return;

        ent.Comp1.NextMove += ent.Comp2.MoveDelay;

        if (wishDir == Vector2.Zero)
        {
            Dirty(ent, ent.Comp1);
            return;
        }

        if (!PipeQuery.TryComp(ent.Comp1.CurrentPipe, out var pipe))
        {
            Log.Warning($"{ToPrettyString(ent.Comp1.CurrentPipe)} does not have a {nameof(PipeCrawlingPipeComponent)} yet {ToPrettyString(ent.Owner)} is crawling inside?");
            Dirty(ent.Owner, ent.Comp1);
            return;
        }

        if (!pipe.ConnectedPipes.TryGetValue(ent.Comp1.CurrentLayer, out var connections))
            return;

        var normalizedWishDir = Vector2.Normalize(wishDir);
        var wishDirection = DirectionExtensions.GetDir(normalizedWishDir);
        if (!connections.TryGetValue(wishDirection, out var netConnection))
            return;

        var connection = GetEntity(netConnection);
        var connectedPipe = PipeQuery.Comp(connection);
        if (!_container.Insert(ent.Owner, connectedPipe.Container))
            return;

        ent.Comp1.CurrentPipe = connection;
        Dirty(ent.Owner, ent.Comp1);

        // random isnt predictable so server it is
        if (!_net.IsServer)
            return;

        if (!_random.Prob(connectedPipe.MovingSoundProb))
            return;

        _audio.PlayPvs(connectedPipe.MovingSound, connection);
    }

    private void Insert(Entity<PipeCrawlingPipeComponent> ent, EntityUid toInsert)
    {
        _container.Insert(toInsert, ent.Comp.Container);
        EnsureComp<PipeCrawlingComponent>(toInsert, out var crawling);
        crawling.CurrentPipe = ent.Owner;
        crawling.CurrentLayer = CompOrNull<AtmosPipeLayersComponent>(ent.Owner)?.CurrentPipeLayer ?? AtmosPipeLayer.Primary;

        SetActionIcon((toInsert, crawling));
    }

    private void Eject(Entity<PipeCrawlingPipeComponent> ent, EntityUid toRemove)
    {
        _container.Remove(toRemove, ent.Comp.Container);
        RemCompDeferred<PipeCrawlingComponent>(toRemove);
    }
}
