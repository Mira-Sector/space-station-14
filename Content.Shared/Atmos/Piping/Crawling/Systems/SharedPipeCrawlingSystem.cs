using Content.Shared.Atmos.Piping.Crawling.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

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
