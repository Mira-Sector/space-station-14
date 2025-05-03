using Content.Shared.Modules.Components;
using Content.Shared.Modules.Events;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Modules;

public abstract partial class ModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeModules();
        InitializePower();
        InitializeRelay();

        SubscribeLocalEvent<ModuleContainerComponent, ComponentInit>(OnContainerInit);

        SubscribeLocalEvent<ModuleContainerComponent, ContainerIsInsertingAttemptEvent>(OnContainerAttempt);

        SubscribeLocalEvent<ModuleContainerComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<ModuleContainerComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnContainerInit(Entity<ModuleContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Modules = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModuleContainerId);
    }

    private void OnContainerAttempt(Entity<ModuleContainerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        var containerEv = new ModuleContainerModuleAddingAttemptEvent(args.EntityUid);
        RaiseLocalEvent(ent.Owner, containerEv);

        var moduleEv = new ModuleAddingAttemptContainerEvent(ent.Owner);
        RaiseLocalEvent(args.EntityUid, moduleEv);

        if (moduleEv.Cancelled || containerEv.Cancelled)
            args.Cancel();
    }

    private void OnContainerInserted(Entity<ModuleContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        var containerEv = new ModuleContainerModuleAddedEvent(args.Entity);
        RaiseLocalEvent(ent.Owner, containerEv);

        var moduleEv = new ModuleAddedContainerEvent(ent.Owner);
        RaiseLocalEvent(args.Entity, moduleEv);
    }

    private void OnContainerRemoved(Entity<ModuleContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        var containerEv = new ModuleContainerModuleRemovedEvent(args.Entity);
        RaiseLocalEvent(ent.Owner, containerEv);

        var moduleEv = new ModuleRemovedContainerEvent(ent.Owner);
        RaiseLocalEvent(args.Entity, moduleEv);
    }

    [PublicAPI]
    public IEnumerable<EntityUid> GetModules(Entity<ModuleContainerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        foreach (var module in ent.Comp.Modules.ContainedEntities)
            yield return module;
    }

    [PublicAPI]
    public void RaiseEventToModules<T>(Entity<ModuleContainerComponent?> ent, T ev) where T : notnull
    {
        foreach (var module in GetModules(ent))
            RaiseLocalEvent(module, ev);
    }
}
