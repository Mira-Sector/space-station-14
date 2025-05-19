using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class StorageModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageModuleComponent, ComponentInit>(OnStorageInit);
        SubscribeLocalEvent<StorageModuleComponent, ComponentRemove>(OnStorageRemove);

        SubscribeLocalEvent<StorageModuleComponent, StorageInteractUsingAttemptEvent>(OnStorageUsingAttempt);
        SubscribeLocalEvent<StorageModuleComponent, StorageInteractAttemptEvent>(OnStorageAttempt);

        SubscribeLocalEvent<StorageModuleComponent, ModuleAddedContainerEvent>(OnStorageAdded);
        SubscribeLocalEvent<StorageModuleComponent, ModuleRemovedContainerEvent>(OnStorageRemoved);
    }

    private void OnStorageInit(Entity<StorageModuleComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent.Owner, StorageModuleComponent.ContainerId);
    }

    private void OnStorageRemove(Entity<StorageModuleComponent> ent, ref ComponentRemove args)
    {
        _container.ShutdownContainer(ent.Comp.Container);
    }

    private void OnStorageUsingAttempt(Entity<StorageModuleComponent> ent, ref StorageInteractUsingAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStorageAttempt(Entity<StorageModuleComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Silent = true;
        args.Cancelled = true;
    }

    private void OnStorageAdded(Entity<StorageModuleComponent> ent, ref ModuleAddedContainerEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<StorageComponent>(ent.Owner, out var moduleStorage))
            return;

        EnsureComp<StorageComponent>(args.Container, out var containerStorage);
        containerStorage.Grid = moduleStorage.Grid;
        containerStorage.MaxItemSize = moduleStorage.MaxItemSize;
        containerStorage.Whitelist = moduleStorage.Whitelist;
        containerStorage.Blacklist = moduleStorage.Blacklist;
        containerStorage.StorageInsertSound = moduleStorage.StorageInsertSound;
        containerStorage.StorageRemoveSound = moduleStorage.StorageRemoveSound;
        containerStorage.StorageOpenSound = moduleStorage.StorageOpenSound;
        containerStorage.StorageCloseSound = moduleStorage.StorageOpenSound;
        containerStorage.SavedLocations = moduleStorage.SavedLocations;

        foreach (var item in ent.Comp.Container.ContainedEntities)
        {
            var location = ent.Comp.Locations[item];

            _storage.InsertAt((args.Container, containerStorage), item, location, out _, null, false, false);
        }

        Dirty(args.Container, containerStorage);
    }

    private void OnStorageRemoved(Entity<StorageModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<StorageComponent>(args.Container, out var containerStorage))
            return;

        ent.Comp.Locations.Clear();
        foreach (var (item, location) in containerStorage.StoredItems)
        {
            if (_container.Insert(item, ent.Comp.Container))
                ent.Comp.Locations[item] = location;
        }

        Dirty(ent);
    }
}
