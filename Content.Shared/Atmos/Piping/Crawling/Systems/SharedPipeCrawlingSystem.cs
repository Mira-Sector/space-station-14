using System.Numerics;
using Content.Shared.Atmos.Piping.Crawling.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string ContainerId = "pipe-crawling";

    protected EntityQuery<PipeCrawlingPipeComponent> PipeQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeEntry();

        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PipeCrawlingPipeComponent, ComponentRemove>(OnRemove);

        PipeQuery = GetEntityQuery<PipeCrawlingPipeComponent>();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PipeCrawlingComponent, CanEnterPipeCrawlingComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var crawling, out var crawler, out var input))
        {
            if (crawling.NextMove > _timing.CurTime)
                continue;

            crawling.NextMove += crawler.MoveDelay;

            if (input.WishDir == Vector2.Zero)
                continue;

            if (!PipeQuery.TryComp(crawling.CurrentPipe, out var pipe))
            {
                Log.Warning($"{ToPrettyString(crawling.CurrentPipe)} does not have a {nameof(PipeCrawlingComponent)} yet {ToPrettyString(uid)} is crawling inside?");
                Dirty(uid, crawling);
                continue;
            }

            if (!pipe.ConnectedPipes.TryGetValue(crawling.CurrentLayer, out var connections))
                continue;

            var wishDir = DirectionExtensions.GetCardinalDir(new Vector2i((int)input.WishDir.X, (int)input.WishDir.Y));
            if (!connections.TryGetValue(wishDir, out var netConnection))
                continue;

            var connection = GetEntity(netConnection);
            var connectedPipe = PipeQuery.Comp(connection);
            if (!_container.Insert(uid, connectedPipe.Container))
                continue;

            crawling.CurrentPipe = connection;
            Dirty(uid, crawling);

            // random isnt predictable so server it is
            if (!_net.IsServer)
                continue;

            if (!_random.Prob(connectedPipe.MovingSoundProb))
                continue;

            _audio.PlayPvs(connectedPipe.MovingSound, connection);
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

    private void Insert(Entity<PipeCrawlingPipeComponent> ent, EntityUid toInsert)
    {
        _container.Insert(toInsert, ent.Comp.Container);
        EnsureComp<PipeCrawlingComponent>(toInsert).CurrentPipe = ent.Owner;
    }

    private void Eject(Entity<PipeCrawlingPipeComponent> ent, EntityUid toRemove)
    {
        _container.Remove(toRemove, ent.Comp.Container);
        RemCompDeferred<PipeCrawlingComponent>(toRemove);
    }
}
