using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string DeployableSlotPrefix = "deployable-";

    private void InitializeDeployable()
    {
        InitializeDeployableRelay();

        SubscribeLocalEvent<ModSuitPartDeployableComponent, ComponentInit>(OnDeployableInit);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ComponentRemove>(OnDeployableRemoved);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotEquippedEvent>(OnDeployableEquipped);
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ClothingGotUnequippedEvent>(OnDeployableUnequipped);

        SubscribeLocalEvent<ModSuitDeployedPartComponent, BeingUnequippedAttemptEvent>(OnDeployedUnequipAttempt);

        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ComponentInit>(OnDeployableInventoryInit);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ComponentRemove>(OnDeployableInventoryRemoved);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartBeforeEquippedEvent>(OnDeployableInventoryBeforeEquipped);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartUnequippedEvent>(OnDeployableInventoryUnequipped);
    }

    #region Deployable

    protected abstract void OnDeployableInit(Entity<ModSuitPartDeployableComponent> ent, ref ComponentInit args);

    private void OnDeployableRemoved(Entity<ModSuitPartDeployableComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Wearer != null)
            UndeployAll(ent, ent.Comp.Wearer.Value);

        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            var ev = new ModSuitDeployablePartUnequippedEvent(ent.Owner, ent.Comp.Wearer, slot);
            RaiseLocalEvent(part, ev);

            if (_net.IsServer)
                Del(part);
        }

        foreach (var (_, container) in ent.Comp.DeployableContainers)
            Container.ShutdownContainer(container);
    }

    private void OnDeployableEquipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.Wearer = args.Wearer;

        var inventoryComp = CompOrNull<InventoryComponent>(args.Wearer);

        // cleanup any mess we forgot to do
        UndeployAll(ent, (args.Wearer, inventoryComp));

        if (inventoryComp == null)
        {
            Dirty(ent);
            return;
        }

        foreach (var (slot, container) in ent.Comp.DeployableContainers)
        {
            if (!_inventory.HasSlot(args.Wearer, slot, inventoryComp))
                continue;

            if (container.ContainedEntity is not { } part)
                continue;

            var beforeEv = new ModSuitDeployablePartBeforeEquippedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, beforeEv);

            if (!_inventory.TryEquip(args.Wearer, part, slot, true, true, true, inventoryComp))
            {
                var failedEv = new ModSuitDeployablePartUnequippedEvent(ent.Owner, args.Wearer, slot);
                RaiseLocalEvent(part, failedEv);

                Container.Insert(part, container);
                continue;
            }

            ent.Comp.DeployedParts.Add(slot, part);

            var afterEv = new ModSuitDeployablePartDeployedEvent(ent.Owner, args.Wearer, slot);
            RaiseLocalEvent(part, afterEv);
        }
    }

    private void OnDeployableUnequipped(Entity<ModSuitPartDeployableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.Wearer = null;
        UndeployAll(ent, args.Wearer);
        Dirty(ent);
    }

    private void OnDeployedUnequipAttempt(Entity<ModSuitDeployedPartComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    internal void UndeployAll(Entity<ModSuitPartDeployableComponent> ent, Entity<InventoryComponent?> wearer)
    {
        foreach (var (slot, part) in ent.Comp.DeployedParts)
        {
            _inventory.TryUnequip(wearer.Owner, slot, true, true, true, wearer.Comp);

            var container = ent.Comp.DeployableContainers[slot];
            Container.Insert(part, container);

            var ev = new ModSuitDeployablePartUnequippedEvent(ent.Owner, wearer.Owner, slot);
            RaiseLocalEvent(part, ev);
        }

        ent.Comp.DeployedParts.Clear();
    }

    protected static string GetDeployableSlotId(string slot)
    {
        return DeployableSlotPrefix + slot;
    }

    [PublicAPI]
    public IEnumerable<EntityUid> GetDeployedParts(Entity<ModSuitPartDeployableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        foreach (var (_, part) in ent.Comp.DeployedParts)
            yield return part;
    }

    [PublicAPI]
    public IEnumerable<EntityUid> GetDeployableParts(Entity<ModSuitPartDeployableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        foreach (var (_, part) in ent.Comp.DeployableParts)
            yield return part;
    }

    #endregion

    #region Inventory

    private void OnDeployableInventoryInit(Entity<ModSuitDeployableInventoryComponent> ent, ref ComponentInit args)
    {
        ent.Comp.StoredItem = Container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnDeployableInventoryRemoved(Entity<ModSuitDeployableInventoryComponent> ent, ref ComponentRemove args)
    {
        Container.ShutdownContainer(ent.Comp.StoredItem);
    }

    private void OnDeployableInventoryBeforeEquipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartBeforeEquippedEvent args)
    {
        Container.EmptyContainer(ent.Comp.StoredItem, true);

        if (!_inventory.TryGetSlotEntity(args.Wearer, args.Slot, out var slotEntity))
            return;

        _inventory.TryUnequip(args.Wearer, args.Slot, true, true, true);
        Container.Insert(slotEntity.Value, ent.Comp.StoredItem);
    }

    private void OnDeployableInventoryUnequipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartUnequippedEvent args)
    {
        if (ent.Comp.StoredItem?.ContainedEntity is not { } containedEntity)
            return;

        if (args.Wearer is not { } wearer)
        {
            Container.Remove(containedEntity, ent.Comp.StoredItem);
            return;
        }

        _inventory.TryEquip(wearer, containedEntity, args.Slot, true, true, true);
    }

    #endregion
}
