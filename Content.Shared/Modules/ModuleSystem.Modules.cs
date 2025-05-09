using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.Modules;

public partial class ModuleSystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    private void InitializeModules()
    {
        SubscribeLocalEvent<AddComponentContainerModuleComponent, ModuleAddedContainerEvent>(OnContainerCompAdded);
        SubscribeLocalEvent<AddComponentContainerModuleComponent, ModuleRemovedContainerEvent>(OnContainerCompRemoved);

        SubscribeLocalEvent<StorageModuleComponent, ModuleAddedContainerEvent>(OnStorageAdded);
        SubscribeLocalEvent<StorageModuleComponent, ModuleRemovedContainerEvent>(OnStorageRemoved);
    }

    #region ContainerComponent

    private void OnContainerCompAdded(Entity<AddComponentContainerModuleComponent> ent, ref ModuleAddedContainerEvent args)
    {
        EntityManager.AddComponents(args.Container, ent.Comp.Components, true);
    }

    private void OnContainerCompRemoved(Entity<AddComponentContainerModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        EntityManager.RemoveComponents(args.Container, ent.Comp.Components);
    }

    #endregion

    #region Storage

    private void OnStorageAdded(Entity<StorageModuleComponent> ent, ref ModuleAddedContainerEvent args)
    {
        if (!TryComp<StorageComponent>(ent.Owner, out var moduleStorage))
            return;

        EnsureComp<StorageComponent>(args.Container, out var containerStorage);
        TransferStorage((ent.Owner, moduleStorage), (args.Container, containerStorage));
    }

    private void OnStorageRemoved(Entity<StorageModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        if (!TryComp<StorageComponent>(args.Container, out var containerStorage))
            return;

        EnsureComp<StorageComponent>(ent.Owner, out var moduleStorage);
        TransferStorage((args.Container, containerStorage), (ent.Owner, moduleStorage));
    }

    private void TransferStorage(Entity<StorageComponent> source, Entity<StorageComponent> target)
    {
        target.Comp.Grid = source.Comp.Grid;
        target.Comp.MaxItemSize = source.Comp.MaxItemSize;
        target.Comp.Whitelist = source.Comp.Whitelist;
        target.Comp.Blacklist = source.Comp.Blacklist;
        target.Comp.StorageInsertSound = source.Comp.StorageInsertSound;
        target.Comp.StorageRemoveSound = source.Comp.StorageRemoveSound;
        target.Comp.StorageOpenSound = source.Comp.StorageOpenSound;
        target.Comp.StorageCloseSound = source.Comp.StorageOpenSound;

        foreach (var (item, location) in source.Comp.StoredItems)
            _storage.InsertAt((target.Owner, target.Comp), item, location, out _, null, false, false);

        target.Comp.SavedLocations = source.Comp.SavedLocations;
        RemComp(source.Owner, source.Comp);
    }

    #endregion
}
