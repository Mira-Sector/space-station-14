using Content.Server.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Robust.Shared.Containers;

namespace Content.Server.Modules.ModSuit;

public partial class ModSuitSystem
{
    private void InitializeDeployable()
    {
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartBeforeEquippedEvent>(OnDeployableInventoryBeforeEquipped);
        SubscribeLocalEvent<ModSuitDeployableInventoryComponent, ModSuitDeployablePartUnequippedEvent>(OnDeployableInventoryUnequipped);
    }

    #region Deployable

    protected override void OnDeployableInit(Entity<ModSuitPartDeployableComponent> ent, ref ComponentInit args)
    {
        foreach (var (slot, partId) in ent.Comp.DeployablePartIds)
        {
            var container = Container.EnsureContainer<ContainerSlot>(ent.Owner, GetDeployableSlotId(slot));

            var part = Spawn(partId);

            if (!Container.Insert(part, container))
            {
                Del(part);
                continue;
            }

            ent.Comp.DeployableContainers.Add(slot, container);
        }

        Dirty(ent);
    }

    #endregion

    #region Inventory

    private void OnDeployableInventoryBeforeEquipped(Entity<ModSuitDeployableInventoryComponent> ent, ref ModSuitDeployablePartBeforeEquippedEvent args)
    {
        ent.Comp.StoredItem = Container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
        Container.EmptyContainer(ent.Comp.StoredItem, true);

        if (!Inventory.TryGetSlotEntity(args.Wearer, args.Slot, out var slotEntity))
            return;

        Inventory.TryUnequip(args.Wearer, args.Slot, true, true);
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

        Inventory.TryEquip(wearer, containedEntity, args.Slot, true, true);
    }

    #endregion
}
